using AutoMapper;
using BuildManagement.Domain.Entities.Enum;
using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.Exceptions;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using Microsoft.EntityFrameworkCore;
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
            entity.Status = MaterialPurchaseStatus.Pending;
            var result = await _materialPurchaseRepository.RegisterAsync(entity);
            await _materialPurchaseRepository.UnitOfWork.SaveChangesAsync();
            return _mapper.Map<ReturnCreateMaterialPurchaseModel>(result);
        }

        public async Task PreAuthorization(Guid materialPurchaseId)
        {
            //TODO: Gerar um pdf com a autorização
            throw new BadRequestException(ErrorsMessages.NotFoundMaterialPurchase, materialPurchaseId.ToString());
            var entity = await _materialPurchaseRepository.FirstAsyncAsTracking(x => x.Id == materialPurchaseId);
            entity.Status = MaterialPurchaseStatus.PreAuthorized;
            await _materialPurchaseRepository.UnitOfWork.SaveChangesAsync();
        }

        public async Task Authorization(Guid materialPurchaseId)
        {
            var entity = await _materialPurchaseRepository.FirstAsyncAsTracking(x => x.Id == materialPurchaseId, include: x => x.Include(m => m.Material)) 
                ?? throw new BadRequestException(ErrorsMessages.NotFoundMaterialPurchase, materialPurchaseId.ToString());
            entity.Status = MaterialPurchaseStatus.Authorized;
            entity.Material.ForEach(x => x.Status = MaterialStatus.Closed);
            await _materialPurchaseRepository.UnitOfWork.SaveChangesAsync();
        }

    }
}
