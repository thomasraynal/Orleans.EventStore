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

        protected CcyEventBase(string ccyPair)
        {
            StreamId = ccyPair;
        }

        public string StreamId { get;  set; }
    }
}
