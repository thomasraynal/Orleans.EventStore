using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IEventWithVersionId : IHasVersionId, IEvent
    {
    }
}
