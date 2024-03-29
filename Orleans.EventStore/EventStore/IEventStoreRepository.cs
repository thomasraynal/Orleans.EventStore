﻿using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public interface IEventStoreRepository : IDisposable
    {
        bool IsConnected { get; }
        Task Connect(TimeSpan timeout);
        bool IsStarted { get; }
        Task<(int version, TAggregate aggregate)> GetAggregate<TKey, TAggregate>(TKey id) where TAggregate : class, IAggregate, new();
        IObservable<IEvent> Observe(string streamId, long? fromIncluding = null, bool rewindAfterDisconnection = false);
        Task CreatePersistentSubscription(string streamId, string group);
        Task<IEnumerable<IEvent>> GetStream(string streamId);
        IObservable<IEvent> ObservePersistentSubscription(string streamId, string group);
        Task SavePendingEvents(string streamId, long expectedVersion, IEnumerable<IEvent> pendingEvents, params KeyValuePair<string, string>[] extraHeaders);
    }
}