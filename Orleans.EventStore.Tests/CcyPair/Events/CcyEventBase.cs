using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore.Tests
{
    public abstract class CcyEventBase : IEvent
    {
        protected CcyEventBase()
        {
        }

        protected CcyEventBase(string ccyPair, long version = -1L)
        {
            StreamId = ccyPair;
            Version = version;
        }

        public string StreamId { get;  set; }
        public long Version { get; set; }
    }
}
