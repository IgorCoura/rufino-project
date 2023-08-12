using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Interfaces.Repositories;
using MaterialPurchase.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace MaterialPurchase.Service.Services.Modify
{
    public class ValidatePurchaseService : IValidatePurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IPermissionsService _permissionsService;
        public ValidatePurchaseService(IPurchaseRepository purchaseRepository, IPermissionsService permissionsService)
        {
            _purchaseRepository = purchaseRepository;
            _permissionsService = permissionsService;
        }

        public async void ValidatePurchaseOrder(Guid purchaseId)
        {
            var purchase = await _purchaseRepository.FirstAsyncAsTracking(x => x.Id == purchaseId, include: i => i.Include(o => o.AuthorizationUserGroups).ThenInclude(o => o.UserAuthorizations))
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(purchaseId), purchaseId.ToString());

            //TODO: Efetuar os Checks

            await _permissionsService.CheckPurchaseAuthorizations(purchase);
            await _purchaseRepository.UnitOfWork.SaveChangesAsync();
        }
    }
}
