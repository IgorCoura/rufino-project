using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.MessageBroker.Bus
{
    public class PublishSubscribe : IPublishSubscribe
    {
        private readonly IBus _bus;

        public PublishSubscribe(IBus bus)
        {
            _bus = bus;
        }

        public async Task PublishMessageAsync<TEntity>(TEntity message, CancellationToken cancellationToken = default) 
        { 
            await _bus.PubSub.PublishAsync<TEntity>(message, cancellationToken);
        }
    }
}
