﻿using Blip.HttpClient.Extensions;
using Blip.HttpClient.Models;
using Blip.HttpClient.Services;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using RestEase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using Take.Blip.Client;
using Take.Blip.Client.Extensions;

namespace Blip.HttpClient.Factories
{
    /// <summary>
    /// Factory that allows the creation of a BlipClient that uses Http or Tcp
    /// </summary>
    public class BlipClientFactory
    {
        private const string KEY_PREFIX = "Key";
        private const string MSGING_BASE_URL = "https://msging.net/";

        /// <summary>
        /// Creates or updates a Service Collection to include BLiP's extensions and any custom Documents, including an <c>ISender</c>
        /// </summary>
        /// <param name="authKey"></param>
        /// <param name="protocol"></param>
        /// <param name="documents"></param>
        /// <param name="serviceCollection"></param>
        public IServiceCollection BuildServiceCollection(string authKey, BlipProtocol protocol, List<Document> documents = null, IServiceCollection serviceCollection = null)
        {
            serviceCollection = serviceCollection ?? new ServiceCollection();

            var documentResolver = new DocumentTypeResolver();
            documentResolver.WithBlipDocuments();

            documents?.ForEach(d => documentResolver.RegisterDocument(d.GetType()));

            var envelopeSerializer = new EnvelopeSerializer(documentResolver);
            serviceCollection.AddSingleton<IEnvelopeSerializer>(envelopeSerializer);

            var sender = BuildBlipClient(authKey, protocol);
            serviceCollection.AddSingleton(sender);

            serviceCollection.RegisterBlipExtensions();

            return serviceCollection;
        }

        /// <summary>
        /// Builds an ISender using the given auth key for the given protocol
        /// </summary>
        /// <param name="authKey"></param>
        /// <param name="protocol"></param>
        public ISender BuildBlipClient(string authKey, BlipProtocol protocol, List<Document> documents = null)
        {
            switch (protocol)
            {
                case BlipProtocol.Http:
                    return BuildHttpClient(authKey, documents);
                case BlipProtocol.Tcp:
                default:
                    return BuildTcpClient(authKey);
            }
        }

        private ISender BuildTcpClient(string authKey)
        {
            return new BlipClientBuilder().UsingAuthorizationKey(authKey)
                                          .UsingRoutingRule(RoutingRule.Instance)
                                          .WithChannelCount(2)
                                          .Build();
        }

        private ISender BuildHttpClient(string authKey, List<Document> documents = null)
        {
            if (authKey.StartsWith(KEY_PREFIX))
            {
                authKey = authKey.Replace(KEY_PREFIX, string.Empty).Trim();
            }

            var documentResolver = new DocumentTypeResolver();
            documentResolver.WithBlipDocuments();

            documents?.ForEach(d => documentResolver.RegisterDocument(d.GetType()));

            var envelopeSerializer = new EnvelopeSerializer(documentResolver);

            var client = new RestClient(MSGING_BASE_URL) {
                JsonSerializerSettings = envelopeSerializer.Settings
            }.For<IBlipHttpClient>();

            client.Authorization = new AuthenticationHeaderValue(KEY_PREFIX, authKey);
            return new BlipHttpClient(client);
        }
    }
}
