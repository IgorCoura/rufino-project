using EasyNetQ.AutoSubscribe;
using EasyNetQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Commom.MessageBroker.Bus;

namespace Commom.MessageBroker
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMessageBrokerConfig(this IServiceCollection services, IConfiguration configuration, string subscriptionIdPrefix) 
        {
            services.AddSingleton<IBus>(RabbitHutch.CreateBus(configuration["MessageBroker:ConnectionString"]));
            services.AddSingleton<MessageDispatcher>();
            services.AddScoped<IPublishSubscribe, PublishSubscribe>();
            services.AddSingleton<AutoSubscriber>(provider =>
            {
                var subscriber = new AutoSubscriber(provider.GetRequiredService<IBus>(), subscriptionIdPrefix)
                {
                    AutoSubscriberMessageDispatcher = provider.GetRequiredService<MessageDispatcher>()
                };
                return subscriber;
            });

            return services;
        }
    }

}
