using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Services
{
    public class ValidatePurchaseService : IValidatePurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IPurchaseService _purchaseService;
        public ValidatePurchaseService(IPurchaseRepository purchaseRepository, IPurchaseService purchaseService)
        {
            _purchaseRepository = purchaseRepository;
            _purchaseService = purchaseService;
        }

        public async void ValidatePurchaseOrder(Guid PurchaseId)
        {
            var purchase = await _purchaseRepository.FirstAsyncAsTracking(x => x.Id == PurchaseId)
                ?? throw new BadRequestException();

            //TODO: Efetuar os Checks

            _purchaseService.CheckPurchaseAuthorizations(purchase);
        }
    }
}
