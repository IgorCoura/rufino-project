using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
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
