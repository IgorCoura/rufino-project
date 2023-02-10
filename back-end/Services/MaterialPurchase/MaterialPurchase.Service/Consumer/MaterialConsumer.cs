using Commom.MessageBroker.Message;
using EasyNetQ.AutoSubscribe;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Consumer
{
    public class MaterialConsumer : IConsumeAsync<ModifyMaterialMessage>, IConsumeAsync<DeleteMaterialMessage>
    {
        public readonly IMaterialRepository _materialRepository;

        public MaterialConsumer(IMaterialRepository materialRepository)
        {
            _materialRepository = materialRepository;
        }

        public async Task ConsumeAsync(ModifyMaterialMessage message, CancellationToken cancellationToken = default)
        {
            var entity = await _materialRepository.FirstAsyncAsTracking(x => x.Id == message.Id);

            if(entity == null)
            {
                entity = new Material()
                {
                    Id = message.Id,
                    Name = message.Name,
                    Unity = message.Unity
                };
                await _materialRepository.RegisterAsync(entity);
            }
            else
            {
                entity.Name = message.Name;
                entity.Unity = message.Unity;   
            }

            await _materialRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ConsumeAsync(DeleteMaterialMessage message, CancellationToken cancellationToken = default)
        {
            var entity = await _materialRepository.FirstAsyncAsTracking(x => x.Id == message.Id)
                ?? throw new ArgumentNullException(nameof(message));
            await _materialRepository.DeleteAsync(entity!);
            await _materialRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        
    }
}
