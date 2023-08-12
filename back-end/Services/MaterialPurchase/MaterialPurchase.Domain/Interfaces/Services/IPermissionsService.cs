using Commom.Domain.Exceptions;
using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Interfaces.Services
{
    public interface IPermissionsService
    {
        Task<PurchaseUserAuthorization?> VerifyPermissions(IEnumerable<PurchaseAuthUserGroup> purchase, Context context, params UserAuthorizationPermissions[] permissions);

        Task VerifyStatus(PurchaseStatus currentStatus, params PurchaseStatus[] statusNotCanBe);

        Task CheckPurchaseAuthorizations(Purchase purchase);
    }
}
