using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IHasVersionId
    {
        long Version { get; set; }
    }
}
