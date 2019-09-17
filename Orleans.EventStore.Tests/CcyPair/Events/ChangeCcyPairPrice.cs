using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore.Tests
{
    [Serializable]
    public class ChangeCcyPairPrice : CcyEventBase
    {
        public ChangeCcyPairPrice()
        {
        }

        public ChangeCcyPairPrice(string ccyPair, string market, double bid, double ask) : base(ccyPair)
        {
            Ask = ask;
            Bid = bid;
            Market = market;
        }

        public double Ask { get;  set; }
        public double Bid { get;  set; }
        public string Market { get;  set; }

        public override string ToString()
        {
            return $"{StreamId} bid:{Bid} ask:{Ask} - {Market}";
        }
    }
}
