using AutoMapper;
using Commom.Domain.Exceptions;
using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Services
{
    public class DraftPurchaseService
    {
        private readonly IMapper _mapper;
        private readonly IConstructionRepository _constructionRepository;
        private readonly IPurchaseRepository _purchaseRepository;

        public DraftPurchaseService(IMapper mapper, IConstructionRepository constructionRepository, IPurchaseRepository purchaseRepository)
        {
            _mapper = mapper;
            _constructionRepository = constructionRepository;
            _purchaseRepository = purchaseRepository;
        }

        public async Task Create(Context context, CreateDraftPurchaseRequest req)
        {
            //Validar 

            var construction = await _constructionRepository.FirstAsync(x => x.Id == req.ConstructionId)
                ?? throw new BadRequestException(); //TODO Colocar error;
            
            var purchase = _mapper.Map<Purchase>(req);

            purchase.AuthorizationUserGroups = GetAuthorizationUserGroups(context, construction);

            await _purchaseRepository.RegisterAsync(purchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();
        }

        public async Task Update(DraftPurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsync(x => x.Id == req.Id, include: i => i.Include(x => x.Materials)) 
                ?? throw new BadRequestException(); //TODO Colocar error;

            if (currentPurchase.Status != PurchaseStatus.Open)
                throw new BadRequestException(); //TODO Colocar error;
                                                 
            //VALIDAR

            _mapper.Map<DraftPurchaseRequest, Purchase>(req, currentPurchase);

            await _purchaseRepository.UpdateAsync(currentPurchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();
        }

        public async Task Delete(Guid PurchaseId)
        {
            var currentPurchase = await _purchaseRepository.FirstAsync(x => x.Id == PurchaseId)
                ?? throw new BadRequestException(); //TODO Colocar error;

            if(currentPurchase.Status != PurchaseStatus.Open)
                throw new BadRequestException(); //TODO Colocar error;

            await _purchaseRepository.DeleteAsync(currentPurchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();
        }

  

        public async Task SendToAuthorization(Guid PurchaseId)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(x => x.Id == PurchaseId)
                ?? throw new BadRequestException(); //TODO Colocar error;

            currentPurchase.Status = PurchaseStatus.Pending;

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();


        }

        private List<AuthorizationUserGroup> GetAuthorizationUserGroups(Context context, Construction construction)
        {
            var authorizationUserGroups = construction.PurchasingAuthorizationUserGroups.ToList();

            var userAuthorizations = new List<UserAuthorization>();

            userAuthorizations.Add(
                new UserAuthorization()
                {
                    UserId = context.User.Id,
                    AuthorizationStatus = UserAuthorizationStatus.Approved,
                    Permissions = UserAuthorizationPermissions.Creator
                });

            authorizationUserGroups.Prepend(new AuthorizationUserGroup()
            {
                UserAuthorizations = userAuthorizations
            });

            return authorizationUserGroups;
        }
        
    }
}
