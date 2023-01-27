using AutoMapper;
using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace MaterialPurchase.Service.Services
{
    public class RecoverPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IMapper _mapper;

        public RecoverPurchaseService(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<SimplePurchaseResponse> SimpleRecover(PurchaseRequest req)
        {
            var result = await _purchaseRepository.FirstAsync(
                filter: x => x.Id == req.Id,
                include: i => i.Include(o => o.Construction).Include(o => o.Provider)!)
                ?? throw new BadRequestException(); //TODO: Colocar error;

            return _mapper.Map<SimplePurchaseResponse>(result);
        }

        public async Task<PurchaseWithMaterialResponse> RecoverPurchaseWithMaterials(PurchaseRequest req)
        {
            var result = await _purchaseRepository.FirstAsync(
                filter: x => x.Id == req.Id, 
                include: i => i.Include(o => o.Construction)
                               .Include(o => o.Provider)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Brand)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Material)!)
                ?? throw new BadRequestException(); //TODO: Colocar error;

            return _mapper.Map<PurchaseWithMaterialResponse>(result);
        }
    }
}
