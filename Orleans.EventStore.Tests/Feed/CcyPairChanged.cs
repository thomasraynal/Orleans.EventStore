using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore.Tests
{
    public class CcyPairChanged : ICcyPair, IEvent
    {
        public CcyPairChanged(string market, string ccyPair, bool isActive, double bid, double ask)
        {
            IsActive = isActive;
            StreamId = ccyPair;
            Ask = ask;
            Bid = bid;
            Date = DateTime.Now;
            Market = market;
        }

        public string Market { get; }

        public string StreamId { get;  }

        public bool IsActive { get; }

        public double Ask { get; }

        public double Bid { get; }

        public DateTime Date { get; }

        public long Version { get; set; }

        public override string ToString()
        {
            return $"{StreamId} bid:{Bid} ask:{Ask} - {Market}";
        }
    }
}
