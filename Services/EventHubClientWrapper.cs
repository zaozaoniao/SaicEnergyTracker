using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Dataflow.SaicEnergyTracker
{
    public class EventHubClientWrapper
    {
        private readonly Lazy<EventHubProducerClient> LazyClient = null;

        public EventHubClientWrapper(IConfiguration configuration,ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<EventHubClientWrapper>();
            var eventhubConnectStr = configuration.GetConnectionString("EventHub");
            var eventhubName = configuration.GetValue<string>("EventHubName");

            if (string.IsNullOrEmpty(eventhubConnectStr))
            {
                logger.LogCritical($"configuration:{nameof(eventhubConnectStr)} is {eventhubConnectStr}?", eventhubConnectStr);
                throw new ArgumentNullException(nameof(eventhubConnectStr));
            }
            if (string.IsNullOrEmpty(eventhubName))
            {
                logger.LogCritical($"configuration:{nameof(eventhubName)} is {eventhubName}?", eventhubName);
                throw new ArgumentNullException(nameof(eventhubName));
            }
           
            LazyClient = new Lazy<EventHubProducerClient>(() =>
            {
                var  client= new EventHubProducerClient(eventhubConnectStr, eventhubName);
                return client;
            });
        }
        public EventHubProducerClient Client => LazyClient.Value;
    }
}
