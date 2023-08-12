using AutoMapper;
using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Interfaces.Repositories;
using MaterialPurchase.Domain.Interfaces.Services;
using MaterialPurchase.Domain.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace MaterialPurchase.Service.Services.Consult
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

        public async Task<SimplePurchaseResponse> SimpleRecover(Guid id)
        {
            var result = await _purchaseRepository.FirstAsync(
                filter: x => x.Id == id,
                include: i => i.Include(o => o.Construction).Include(o => o.Provider)!)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            return _mapper.Map<SimplePurchaseResponse>(result);
        }

        public async Task<PurchaseWithMaterialResponse> RecoverPurchaseWithMaterials(Guid id)
        {
            var result = await _purchaseRepository.FirstAsync(
                filter: x => x.Id == id,
                include: i => i.Include(o => o.Construction)
                               .Include(o => o.Provider)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Brand)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Material)!)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            return _mapper.Map<PurchaseWithMaterialResponse>(result);
        }

        public async Task<CompletePurchaseResponse> RecoverPurchaseComplete(Guid id)
        {
            var result = await _purchaseRepository.FirstAsync(
                filter: x => x.Id == id,
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
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            return _mapper.Map<CompletePurchaseResponse>(result);
        }

        public async Task<IEnumerable<CompletePurchaseResponse>> RecoverAllPurchaseComplete()
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
                               .ThenInclude(o => o.Receiver)!);

            return _mapper.Map<IEnumerable<CompletePurchaseResponse>>(result);
        }

        public async Task<IEnumerable<PurchaseWithMaterialResponse>> RecoverAllPurchaseWithMaterials()
        {
            var result = await _purchaseRepository.GetDataAsync(
                include: i => i.Include(o => o.Construction)
                               .Include(o => o.Provider)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Brand)
                               .Include(o => o.Materials)
                               .ThenInclude(o => o.Material)!);

            return _mapper.Map<IEnumerable<PurchaseWithMaterialResponse>>(result);
        }

        public async Task<IEnumerable<SimplePurchaseResponse>> SimpleRecoverAll()
        {
            var result = await _purchaseRepository.GetDataAsync(
                include: i => i.Include(o => o.Construction).Include(o => o.Provider)!);

            return _mapper.Map<IEnumerable<SimplePurchaseResponse>>(result);
        }
    }
}
