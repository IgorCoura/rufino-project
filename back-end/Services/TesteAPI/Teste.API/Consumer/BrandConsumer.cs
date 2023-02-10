using Commom.MessageBroker.Message;
using EasyNetQ.AutoSubscribe;

namespace Teste.API.Consumer
{
    public class BrandConsumer :  IConsumeAsync<ModifyBrandMessage>
    {
        public Task ConsumeAsync(ModifyBrandMessage message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Name: {message.Name}");
            return Task.CompletedTask;
        }

    }
}
