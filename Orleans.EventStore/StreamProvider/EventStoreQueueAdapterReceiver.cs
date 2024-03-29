﻿using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public class EventStoreQueueAdapterReceiver : IQueueAdapterReceiver
    {

        private IDisposable _cleanUp;
        private readonly ConcurrentQueue<IBatchContainer> _receivedMessages;
        private readonly string _providerName;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly QueueId _queueId;

        public EventStoreQueueAdapterReceiver(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory, QueueId queueId, string providerName)
        {
            _receivedMessages = new ConcurrentQueue<IBatchContainer>();
            _providerName = providerName;
            _eventStoreRepository = eventStoreRepository;
            _queueId = queueId;
        }

        public static IQueueAdapterReceiver Create(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory, QueueId queueId, string providerName)
        {
            return new EventStoreQueueAdapterReceiver(eventStoreRepository, loggerFactory, queueId, providerName);
        }

        public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            IList<IBatchContainer> containerList = new List<IBatchContainer>();

            while (containerList.Count < maxCount && _receivedMessages.TryDequeue(out IBatchContainer container))
            {
                containerList.Add(container);
            }

            return Task.FromResult(containerList);
        }

        public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            //todo: handle ack for persistent subscriptions
            return Task.CompletedTask;
        }

        public Task Shutdown(TimeSpan timeout)
        {
            if (null != _cleanUp) _cleanUp.Dispose();

            return Task.CompletedTask;
        }

        public async Task Initialize(TimeSpan timeout)
        {
            if (!_eventStoreRepository.IsStarted)
            {
                await _eventStoreRepository.Connect(timeout);
            }

            _cleanUp = _eventStoreRepository.Observe(_providerName)
                .Subscribe(ev =>
                {
                    Debug.WriteLine($"{ev.StreamId} {ev.Version}");
                    _receivedMessages.Enqueue(new EventStoreBatchContainer(Guid.Empty, ev.StreamId, ev, new EventStoreStreamSequenceToken(ev.Version)));
                });
        }


    }
}
