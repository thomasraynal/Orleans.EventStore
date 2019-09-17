using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore.Tests
{
    public class MarketGrain : Producer<CcyPairChanged>, IMarketGrain
    {
        public async Task Tick(string ccyPair, double bid, double ask)
        {
            await Next(new CcyPairChanged(this.GetPrimaryKeyString(), ccyPair, true, bid, ask));
        }
    }
}
