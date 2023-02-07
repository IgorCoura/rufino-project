using AutoMapper;
using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.BaseEntities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Errors;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MaterialPurchase.Service.Services
{
    public class DraftPurchaseService : IDraftPurchaseService
    {
        private readonly IMapper _mapper;
        private readonly IConstructionRepository _constructionRepository;
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IValidatePurchaseService _validatePurchaseService;
        private readonly IPermissionsService _permissionService;
        private readonly IServiceProvider _serviceProvider;

        public DraftPurchaseService(IMapper mapper, IConstructionRepository constructionRepository, IPurchaseRepository purchaseRepository, IValidatePurchaseService validatePurchaseService, IPermissionsService permissionService, IServiceProvider serviceProvider)
        {
            _mapper = mapper;
            _constructionRepository = constructionRepository;
            _purchaseRepository = purchaseRepository;
            _validatePurchaseService = validatePurchaseService;
            _permissionService = permissionService;
            _serviceProvider = serviceProvider;
        }

        public async Task<PurchaseResponse> Create(Context context, CreateDraftPurchaseRequest req)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<CreateDraftPurchaseRequest>>().ValidateAsync(req);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var construction = await _constructionRepository.FirstAsync(
                filter: x => x.Id == req.ConstructionId, 
                include: i => i.Include(o => o.PurchasingAuthorizationUserGroups).ThenInclude(o=> o.UserAuthorizations))
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.ConstructionId), req.ConstructionId.ToString());

            var authorizationUserGroups = ConvertAuthorizationUserGroups(context, construction.PurchasingAuthorizationUserGroups);

            await _permissionService.VerifyPermissions(authorizationUserGroups, context, UserAuthorizationPermissions.Creator, UserAuthorizationPermissions.Admin);

            var purchase = _mapper.Map<Purchase>(req);

            purchase.AuthorizationUserGroups = authorizationUserGroups;

            try
            {
                var result = await _purchaseRepository.RegisterAsync(purchase);

                await _purchaseRepository.UnitOfWork.SaveChangesAsync();

                return _mapper.Map<PurchaseResponse>(result);
            }
            catch (ReferenceConstraintException ex)
            {
                throw new BadRequestException(CommomErrors.ReferenceConstraintViolation);
            };

        }

        public async Task<PurchaseResponse> Update(Context context, DraftPurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                filter: x => x.Id == req.Id, 
                include: i => i.Include(x => x.Materials).Include(o => o.AuthorizationUserGroups).ThenInclude(o => o.UserAuthorizations)) 
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            await _permissionService.VerifyPermissions(currentPurchase.AuthorizationUserGroups, context, UserAuthorizationPermissions.Creator, UserAuthorizationPermissions.Admin);

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.Open);

            var validation = await _serviceProvider.GetRequiredService<IValidator<DraftPurchaseRequest>>().ValidateAsync(req);

            if(!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            foreach (var mat in req.Materials)
            {
                var currentMaterial = currentPurchase!.Materials.Where(x => x.Id == mat.Id).FirstOrDefault();
                if (currentMaterial == null)
                    continue;

                currentMaterial.Quantity = mat.Quantity;
                currentMaterial.UnitPrice = mat.UnitPrice;
                currentMaterial.MaterialId = mat.MaterialId;
                currentMaterial.BrandId = mat.BrandId;
            }

            currentPurchase.ProviderId = req.ProviderId;
            currentPurchase.ConstructionId= req.ConstructionId;
            currentPurchase.Freight= req.Freight;

            try
            {
                await _purchaseRepository.UnitOfWork.SaveChangesAsync();

                return _mapper.Map<PurchaseResponse>(currentPurchase);
            }
            catch (ReferenceConstraintException ex)
            {
                throw new BadRequestException(CommomErrors.ReferenceConstraintViolation);
            };  
        }

        public async Task Delete(Context context, PurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsync(
                filter: x => x.Id == req.Id, 
                include: i => i.Include(o => o.AuthorizationUserGroups).ThenInclude(o => o.UserAuthorizations))
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            await _permissionService.VerifyPermissions(currentPurchase.AuthorizationUserGroups, context, UserAuthorizationPermissions.Creator, UserAuthorizationPermissions.Admin);

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.Open);

            await _purchaseRepository.DeleteAsync(currentPurchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

        }

  

        public async Task<PurchaseResponse> SendToAuthorization(Context context, PurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                filter: x => x.Id == req.Id, 
                include: i => i.Include(o => o.AuthorizationUserGroups).ThenInclude(o => o.UserAuthorizations))
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            await _permissionService.VerifyPermissions(currentPurchase.AuthorizationUserGroups, context, UserAuthorizationPermissions.Creator, UserAuthorizationPermissions.Admin);

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.Open);

            currentPurchase.Status = PurchaseStatus.Pending;

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            _validatePurchaseService.ValidatePurchaseOrder(req.Id);

            return _mapper.Map<PurchaseResponse>(currentPurchase);
        }



        private static List<PurchaseAuthUserGroup> ConvertAuthorizationUserGroups(Context context, IEnumerable<ConstructionAuthUserGroup> constructionAuthorizationUserGroups)
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
                    }).ToList()
                };
            }).ToList();

            return authorizationUserGroups;

        }
        
    }
}
