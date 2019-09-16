using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IAggregate
    {
        void Apply(IEvent @event);
    }
}
