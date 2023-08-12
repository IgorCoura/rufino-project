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

        public Task<PurchaseUserAuthorization?> VerifyPermissions(IEnumerable<PurchaseAuthUserGroup> purchase, Context context, params UserAuthorizationPermissions[] permissions)
        {
            var orderGroup = purchase.OrderBy(x => x.Priority).ToList();
            var user = FindUserAuthorization(context.User.Id, orderGroup);

            if (user == null)
            {
                if (context.User.FunctionsId.Any(x => x == MaterialPurchaseAuthorizationId.ByPassPurchasePermission))
                    return Task.FromResult<PurchaseUserAuthorization?>(null);
                else
                    throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);

            }

            if (permissions.Any(x => x == user.Permissions))
            {
                if (user.Permissions == UserAuthorizationPermissions.Client)
                {
                    HasUserPedingWithHighestPriority(context.User.Id, orderGroup);
                }
                return Task.FromResult<PurchaseUserAuthorization?>(user);
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

        private static void HasUserPedingWithHighestPriority(Guid id, List<PurchaseAuthUserGroup> orderGroup)
        {
            foreach (var group in orderGroup)
            {
                if (group.UserAuthorizations.Any(x => x.UserId == id))
                    return;

                if (group.UserAuthorizations.Any(x => x.AuthorizationStatus == UserAuthorizationStatus.Pending))
                    throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);

            }
            throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);
        }

        private static PurchaseUserAuthorization? FindUserAuthorization(Guid id, List<PurchaseAuthUserGroup> orderGroup)
        {
            foreach (var group in orderGroup)
            {
                var userAuth = group.UserAuthorizations.Where(x => x.UserId == id).FirstOrDefault();

                if (userAuth != null)
                    return userAuth;
            }
            return null;
        }
    }
}
