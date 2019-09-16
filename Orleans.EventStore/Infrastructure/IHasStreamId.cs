using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IHasStreamId
    {
        string StreamId { get; }
    }
}
