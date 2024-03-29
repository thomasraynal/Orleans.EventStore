﻿using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore.Tests
{
    public interface ICcyPair
    {
        bool IsActive { get; }
        double Ask { get; }
        double Bid { get; }
    }
}
