using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.MessageBroker.Bus
{
    public interface IPublishSubscribe
    {
        Task PublishMessageAsync<TEntity>(TEntity message, CancellationToken cancellationToken = default);
    }
}
