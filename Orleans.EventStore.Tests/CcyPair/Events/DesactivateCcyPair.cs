using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore.Tests
{
    [Serializable]
    public class DesactivateCcyPair : CcyEventBase
    {
        public DesactivateCcyPair()
        {
        }

        public DesactivateCcyPair(string ccyPair) : base(ccyPair)
        {
        }
    }
}
