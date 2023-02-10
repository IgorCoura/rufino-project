using Commom.MessageBroker.Bus;

namespace Commom.Tests.Mocks
{
    public class PublishSubscribeMock : IPublishSubscribe
    {
        public Task PublishMessageAsync<TEntity>(TEntity message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
