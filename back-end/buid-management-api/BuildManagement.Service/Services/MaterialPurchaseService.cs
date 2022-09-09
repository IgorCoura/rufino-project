using AutoMapper;
using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Services
{
    public class MaterialPurchaseService : IMaterialPurchaseService
    {
        private readonly IMaterialPurchaseRepository _materialPurchaseRepository;
        private readonly IMapper _mapper;

        public MaterialPurchaseService(IMaterialPurchaseRepository orderPurchaseRepository, IMapper mapper)
        {
            _materialPurchaseRepository = orderPurchaseRepository;
            _mapper = mapper;
        }

        public async Task<ReturnCreateMaterialPurchaseModel> Create(CreateMaterialPurchaseModel model)
        {
            var entity = _mapper.Map<MaterialPurchase>(model);
            var result = await _materialPurchaseRepository.RegisterAsync(entity);
            await _materialPurchaseRepository.UnitOfWork.SaveChangesAsync();
            return _mapper.Map<ReturnCreateMaterialPurchaseModel>(result);
        }

    }
}
