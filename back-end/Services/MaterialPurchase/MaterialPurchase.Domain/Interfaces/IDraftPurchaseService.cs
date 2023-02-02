using Commom.Domain.Exceptions;
using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.BaseEntities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Interfaces
{
    public interface IDraftPurchaseService
    {
        Task<PurchaseResponse> Create(Context context, CreateDraftPurchaseRequest req);
        Task<PurchaseResponse> Update(Context context, DraftPurchaseRequest req);
        Task Delete(Context context, PurchaseRequest req);
        Task<PurchaseResponse> SendToAuthorization(Context context, PurchaseRequest req);
    }
}
