﻿using Orleans.Providers.Streams.Common;
using Orleans.Streams;
using System;

namespace Orleans.EventStore
{
    [Serializable]
    public class EventStoreStreamSequenceToken : EventSequenceToken
    {
        public long EventNumber { get; }

        public EventStoreStreamSequenceToken(long eventNumber) : base(eventNumber, (int)eventNumber)
        {
            EventNumber = eventNumber;
        }

        public override bool Equals(StreamSequenceToken other)
        {
            return (other as EventStoreStreamSequenceToken)?.EventNumber == EventNumber;
        }

        public override int CompareTo(StreamSequenceToken other)
        {
            return EventNumber.CompareTo(((EventStoreStreamSequenceToken)other).EventNumber);
        }
    }
}
