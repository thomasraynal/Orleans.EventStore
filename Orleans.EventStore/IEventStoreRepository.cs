using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public interface IEventStoreRepository
    {
        bool IsConnected { get; }
        Task Connect(TimeSpan timeout);
        bool IStarted { get; }
        Task<(int version, TAggregate aggregate)> GetAggregate<TKey, TAggregate>(TKey id) where TAggregate : class, IAggregate, new();
        IObservable<IEvent> Observe(string streamId, long? fromIncluding = null, bool rewindAfterDisconnection = false);
        Task CreatePersistentSubscription(string streamId, string group);
        Task<IEnumerable<IEvent>> GetStream(string streamId);
        Task<StreamMetadataResult> GetStreamMetadata(string streamId);
        IObservable<IEvent> ObservePersistentSubscription(string streamId, string group);
        Task SavePendingEvents(string streamId, long originalVersion, IEnumerable<IEvent> pendingEvents, params KeyValuePair<string, string>[] extraHeaders);
    }
}