using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IMaterialPurchaseService
    {
        Task<ReturnCreateMaterialPurchaseModel> Create(CreateMaterialPurchaseModel model);
    }
}
