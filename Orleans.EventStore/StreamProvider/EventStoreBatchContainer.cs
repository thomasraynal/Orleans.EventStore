﻿using Orleans.Streams;
using System;
using System.Collections.Generic;

namespace Orleans.EventStore
{
    [Serializable]
    public class EventStoreBatchContainer : IBatchContainer
    {
        public Guid StreamGuid { get; }
        public string StreamNamespace { get; }
        public IEvent Event { get; }

        public EventStoreStreamSequenceToken EventStoreStreamSequenceToken { get; }
        public StreamSequenceToken SequenceToken => EventStoreStreamSequenceToken as StreamSequenceToken;

        public EventStoreBatchContainer(Guid streamGuid, string streamNamespace, IEvent @event, EventStoreStreamSequenceToken eventStoreStreamSequenceToken )
        {
            Event = @event;
            EventStoreStreamSequenceToken = eventStoreStreamSequenceToken;
            StreamGuid = streamGuid;
            StreamNamespace = streamNamespace;
        }

        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        {
            yield return new Tuple<T, StreamSequenceToken>((T)Event, null);
        }

        public bool ImportRequestContext()
        {
            return true;
        }

        public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc)
        {
            return true;
        }
    }
}
