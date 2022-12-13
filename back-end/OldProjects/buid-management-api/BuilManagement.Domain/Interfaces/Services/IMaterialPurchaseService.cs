using BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using BuildManagement.Domain.Models.Purchase.MaterialReceive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IMaterialPurchaseService
    {
        Task<CreateMaterialPurchaseResponse> CreateMaterialPurchase(CreateMaterialPurchaseRequest model);
        Task PreAuthorization(Guid id);
        Task Authorization(Guid id);
        Task<MaterialReceiveResponse> MaterialReceive(MaterialReceiveRequest model);

        Task<ReturnMaterialPurchaseModel> Get(Guid id);
    }
}
