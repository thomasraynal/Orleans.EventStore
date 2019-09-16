using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore.Tests
{
    [Serializable]
    public class ActivateCcyPair : CcyEventBase
    {
        public ActivateCcyPair()
        {
        }

        public ActivateCcyPair(string ccyPair) : base(ccyPair)
        {
        }
    }
}
