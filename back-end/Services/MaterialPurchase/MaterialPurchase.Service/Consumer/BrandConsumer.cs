using Commom.MessageBroker.Message;
using EasyNetQ.AutoSubscribe;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Interfaces.Repositories;

namespace MaterialPurchase.Service.Consumer
{
    public class BrandConsumer : IConsumeAsync<ModifyBrandMessage>, IConsumeAsync<DeleteBrandMessage>
    {
        private readonly IBrandRepository _brandRepository;

        public BrandConsumer(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }

        public async Task ConsumeAsync(ModifyBrandMessage message, CancellationToken cancellationToken = default)
        {           
            var entity = await _brandRepository.FirstAsyncAsTracking(x => x.Id == message.Id);

            if(entity == null) {
                entity = new Brand()
                {
                    Id = message.Id,
                    Name = message.Name
                };
                await _brandRepository.RegisterAsync(entity);
            }
            else
            {
                entity.Id = message.Id;
                entity.Name= message.Name;                
            }

            await _brandRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ConsumeAsync(DeleteBrandMessage message, CancellationToken cancellationToken = default)
        {
            var entity = await _brandRepository.FirstAsyncAsTracking(x => x.Id == message.Id)
                ?? throw new ArgumentNullException(nameof(message));
            await _brandRepository.DeleteAsync(entity);           
            await _brandRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

    }
}
