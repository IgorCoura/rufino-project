using Commom.Domain.Exceptions;
using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Errors;
using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Interfaces.Services;

namespace MaterialPurchase.Service.Services.Modify
{
    public class PermissionsService : IPermissionsService
    {
        public PermissionsService()
        {
        }

        public Task<PurchaseUserAuthorization> FindUserAuthorization(Guid id, IEnumerable<PurchaseAuthUserGroup> purchase)
        {
            var orderGroup = purchase.OrderBy(x => x.Priority).ToList();
            foreach (var group in orderGroup)
            {
                var userAuth = group.UserAuthorizations.Where(x => x.UserId == id).FirstOrDefault();

                if (userAuth != null)
                    return Task.FromResult(userAuth);

                var hasPendingAuthorization = group.UserAuthorizations.Any(x => x.AuthorizationStatus == UserAuthorizationStatus.Pending);

                if (hasPendingAuthorization)
                    throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);
            }
            throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);
        }

        public Task VerifyStatus(PurchaseStatus currentStatus, params PurchaseStatus[] statusHaveBe)
        {
            if (!statusHaveBe.Any(x => x == currentStatus))
                throw new BadRequestException(MaterialPurchaseErrors.PurchaseStatusInvalid, currentStatus.ToString());

            return Task.CompletedTask;
        }

        public Task CheckPurchaseAuthorizations(Purchase purchase)
        {
            if (purchase.Status != PurchaseStatus.Pending && purchase.Status != PurchaseStatus.Authorizing)
                throw new BadRequestException(MaterialPurchaseErrors.PurchaseStatusInvalid, purchase.Status.ToString());

            var needAuthorization = purchase.AuthorizationUserGroups.Any(x => x.UserAuthorizations.Any(u => u.AuthorizationStatus == UserAuthorizationStatus.Pending));

            if (needAuthorization)
            {
                purchase.Status = PurchaseStatus.Authorizing;
                return Task.CompletedTask;
            }
            else
            {
                var WasReproved = purchase.AuthorizationUserGroups.Any(x => x.UserAuthorizations.Any(u => u.AuthorizationStatus == UserAuthorizationStatus.Reproved));
                if (WasReproved)
                {
                    purchase.Status = PurchaseStatus.Cancelled;
                    return Task.CompletedTask;
                }
            }
            purchase.Status = PurchaseStatus.Approved;
            return Task.CompletedTask;
        }


       
    }
}
