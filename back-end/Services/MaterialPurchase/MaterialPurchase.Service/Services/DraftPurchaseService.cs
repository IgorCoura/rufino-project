using AutoMapper;
using Commom.Domain.Exceptions;
using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Services
{
    public class DraftPurchaseService : IDraftPurchaseService
    {
        private readonly IMapper _mapper;
        private readonly IConstructionRepository _constructionRepository;
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IValidatePurchaseService _validatePurchaseService;

        public DraftPurchaseService(IMapper mapper, IConstructionRepository constructionRepository, IPurchaseRepository purchaseRepository, IValidatePurchaseService validatePurchaseService)
        {
            _mapper = mapper;
            _constructionRepository = constructionRepository;
            _purchaseRepository = purchaseRepository;
            _validatePurchaseService = validatePurchaseService;
        }

        public async Task<PurchaseResponse> Create(Context context, CreateDraftPurchaseRequest req)
        {
            //Validar 

            var construction = await _constructionRepository.FirstAsync(x => x.Id == req.ConstructionId)
                ?? throw new BadRequestException(); //TODO Colocar error;
            
            var purchase = _mapper.Map<Purchase>(req);

            purchase.AuthorizationUserGroups = GetAuthorizationUserGroups(context,construction.PurchasingAuthorizationUserGroups);

            var result = await _purchaseRepository.RegisterAsync(purchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(result);
        }

        public async Task<PurchaseResponse> Update(DraftPurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsync(x => x.Id == req.Id, include: i => i.Include(x => x.Materials)) 
                ?? throw new BadRequestException(); //TODO Colocar error;

            if (currentPurchase.Status != PurchaseStatus.Open)
                throw new BadRequestException(); //TODO Colocar error;
                                                 
            //VALIDAR

            _mapper.Map<DraftPurchaseRequest, Purchase>(req, currentPurchase);

            var result = await _purchaseRepository.UpdateAsync(currentPurchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(result);
        }

        public async Task Delete(PurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsync(x => x.Id == req.Id)
                ?? throw new BadRequestException(); //TODO Colocar error;

            if(currentPurchase.Status != PurchaseStatus.Open)
                throw new BadRequestException(); //TODO Colocar error;

            await _purchaseRepository.DeleteAsync(currentPurchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

        }

  

        public async Task<PurchaseResponse> SendToAuthorization(PurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(x => x.Id == req.Id)
                ?? throw new BadRequestException(); //TODO Colocar error;

            currentPurchase.Status = PurchaseStatus.Pending;

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            _validatePurchaseService.ValidatePurchaseOrder(req.Id);

            return _mapper.Map<PurchaseResponse>(currentPurchase);
        }



        private static IEnumerable<PurchaseAuthUserGroup> GetAuthorizationUserGroups(Context context, IEnumerable<ConstructionAuthUserGroup> constructionAuthorizationUserGroups)
        {
            var authorizationUserGroups = constructionAuthorizationUserGroups.Select(group =>
            {
                return new PurchaseAuthUserGroup()
                {
                    Priority = group.Priority,
                    UserAuthorizations = group.UserAuthorizations.Select(u =>
                    {
                        return new PurchaseUserAuthorization()
                        {
                            UserId = u.UserId,
                            AuthorizationStatus = u.AuthorizationStatus,
                            Permissions = u.Permissions
                        };
                    })
                };
            }).ToList();

            return authorizationUserGroups.Prepend(new PurchaseAuthUserGroup()
            {
                Priority = 0,
                UserAuthorizations = new List<PurchaseUserAuthorization>()
                {
                    new PurchaseUserAuthorization()
                    {
                        UserId = context.User.Id,
                        AuthorizationStatus = UserAuthorizationStatus.Approved,
                        Permissions = UserAuthorizationPermissions.Creator
                    }
                }
            });

        }
        
    }
}
