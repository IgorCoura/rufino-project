using AutoMapper;
using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace MaterialPurchase.Service.Services
{
    public class RecoverPurchaseService : IRecoverPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IMapper _mapper;

        public RecoverPurchaseService(IPurchaseRepository purchaseRepository, IMapper mapper)
        {
            _purchaseRepository = purchaseRepository;
            _mapper = mapper;
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

        public async Task<CompletePurchaseResponse> RecoverPurchaseComplete(PurchaseRequest req)
        {
            var result = await _purchaseRepository.FirstAsync(
                filter: x => x.Id == req.Id,
                include: i => i.Include(o => o.Construction)
                               .Include(o => o.Provider)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Brand)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Material)
                               .Include(o => o.AuthorizationUserGroups)
                               .ThenInclude(o => o.UserAuthorizations)
                               .Include(o => o.PurchaseDeliveries)
                               .ThenInclude(o => o.Receiver)!)
                ?? throw new BadRequestException(); //TODO: Colocar error;

            return _mapper.Map<CompletePurchaseResponse>(result);
        }

        public async Task<IEnumerable<CompletePurchaseResponse>> RecoverPurchaseAllComplete()
        {
            var result = await _purchaseRepository.GetDataAsync(
                include: i => i.Include(o => o.Construction)
                               .Include(o => o.Provider)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Brand)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Material)
                               .Include(o => o.AuthorizationUserGroups)
                               .ThenInclude(o => o.UserAuthorizations)
                               .Include(o => o.PurchaseDeliveries)
                               .ThenInclude(o => o.Receiver)!)
                ?? throw new BadRequestException(); //TODO: Colocar error;

            return _mapper.Map<IEnumerable<CompletePurchaseResponse>>(result);
        }
    }
}
