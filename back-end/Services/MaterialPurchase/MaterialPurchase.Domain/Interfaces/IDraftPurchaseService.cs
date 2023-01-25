using Commom.Domain.Exceptions;
using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Models;
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
        Task<PurchaseResponse> Update(DraftPurchaseRequest req);
        Task Delete(PurchaseRequest req);
        Task<PurchaseResponse> SendToAuthorization(PurchaseRequest req);
    }
}
