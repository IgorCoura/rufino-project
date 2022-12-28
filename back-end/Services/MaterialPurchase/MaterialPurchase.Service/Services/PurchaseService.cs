using MaterialPurchase.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Services
{
    public class PurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public PurchaseService(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task Read()
        {
            throw new NotImplementedException();
        }

        public async Task ApprovePurchase(Guid purchaseId)
        {
             var currentPuchase = await _purchaseRepository.FirstAsync(x => x.Id == purchaseId) ;
        }

        public async Task UnlockPurchase()
        {
            throw new NotImplementedException();
        }

        public async Task ConfirmDelivery()
        {
            throw new NotImplementedException();
        }

        public async Task ReceiveDelivery()
        {
            throw new NotImplementedException();
        }

        public async Task CancelDelivery()
        {
            throw new NotImplementedException();
        }

        public async Task CancelDeliverySubstitucte()
        {
            throw new NotImplementedException();
        }
    }
}
