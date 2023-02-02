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

namespace MaterialPurchase.Service.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;

        private readonly IMapper _mapper;
        private readonly IPermissionsService _permissionService;

        public PurchaseService(IPurchaseRepository purchaseRepository, IMapper mapper, IPermissionsService permissionService)
        {
            _purchaseRepository = purchaseRepository;
            _mapper = mapper;
            _permissionService = permissionService;
        }

        public async Task<PurchaseResponse> AuthorizePurchase(Context context, PurchaseRequest req)
        {
             var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                 filter: x => x.Id == req.Id, 
                 include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations)) 
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            var userAuth = await _permissionService.VerifyPermissions(currentPurchase.AuthorizationUserGroups, context, UserAuthorizationPermissions.Client, UserAuthorizationPermissions.Supervisor ,UserAuthorizationPermissions.Admin)
                ?? throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.Authorizing);

            userAuth.AuthorizationStatus = UserAuthorizationStatus.Approved;
            await _permissionService.CheckPurchaseAuthorizations(currentPurchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPurchase);
        }

        public async Task<PurchaseResponse> UnlockPurchase(Context context, PurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                  filter: x => x.Id == req.Id,
                  include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                 ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());


            await _permissionService.VerifyPermissions(currentPurchase.AuthorizationUserGroups, context, UserAuthorizationPermissions.Supervisor, UserAuthorizationPermissions.Admin);

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.Blocked);

            currentPurchase.Status = PurchaseStatus.Pending;
            await _permissionService.CheckPurchaseAuthorizations(currentPurchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPurchase);
        }

        public async Task<PurchaseResponse> ConfirmDeliveryDate(Context context, ConfirmDeliveryDateRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                  filter: x => x.Id == req.PurchaseId,
                  include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                 ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString());

            await _permissionService.VerifyPermissions(currentPurchase.AuthorizationUserGroups, context, UserAuthorizationPermissions.Receiver, UserAuthorizationPermissions.Creator, UserAuthorizationPermissions.Admin);

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.Approved, PurchaseStatus.DeliveryProblem);

            currentPurchase.Status = PurchaseStatus.WaitingDelivery;
            currentPurchase.LimitDeliveryDate = req.LimitDeliveryDate;

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPurchase);
        }

        public async Task<PurchaseResponse> ReceiveDelivery(Context context, ReceiveDeliveryRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                  filter: x => x.Id == req.PurchaseId,
                  include: i => i.Include(a => a.PurchaseDeliveries).Include(c => c.Materials))
                 ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString());

            await _permissionService.VerifyPermissions(currentPurchase.AuthorizationUserGroups, context, UserAuthorizationPermissions.Receiver, UserAuthorizationPermissions.Creator, UserAuthorizationPermissions.Admin);

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.WaitingDelivery);

            var allIsDelivered = AllMaterialWasDelivered(currentPurchase, req);

            var newDeliveries = req.ReceiveDeliveryItemRequests.Select(item =>
            {
                return new PurchaseDeliveryItem()
                {
                    ReceiverId = context.User.Id,
                    MaterialPurchaseId = item.MaterialPurchaseId,
                    Quantity = item.Quantity,
                };
            });

            var listDeliveries = currentPurchase.PurchaseDeliveries.ToList();
            listDeliveries.AddRange(newDeliveries);

            if (allIsDelivered)
                currentPurchase.Status = PurchaseStatus.Closed;

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPurchase);
        }


        public async Task<PurchaseResponse> CancelPurchaseBeforeAuthorize(Context context, CancelPurchaseRequest req)
        {
            var status = new PurchaseStatus[]
            {
                PurchaseStatus.Pending,
                PurchaseStatus.Blocked
            };
            var permissions = new UserAuthorizationPermissions[]
            {
                UserAuthorizationPermissions.Creator,
                UserAuthorizationPermissions.Supervisor,
                UserAuthorizationPermissions.Admin
            };
            var result = await CancelPurchase(context, req, status, permissions);
            return _mapper.Map<PurchaseResponse>(result);
        }

        public async Task<PurchaseResponse> CancelPurchaseDuringAuthorize(Context context, CancelPurchaseRequest req)
        {
            var status = new PurchaseStatus[]
            {
                PurchaseStatus.Authorizing
            };
            var permissions = new UserAuthorizationPermissions[]
            {
                UserAuthorizationPermissions.Creator,
                UserAuthorizationPermissions.Client,
                UserAuthorizationPermissions.Supervisor,
                UserAuthorizationPermissions.Admin
            };
            var result = await CancelPurchase(context, req,  status, permissions);
            return _mapper.Map<PurchaseResponse>(result);
        }

        public async Task<PurchaseResponse> CancelPurchaseAfterAuthorize(Context context, CancelPurchaseRequest req)
        {
            var status = new PurchaseStatus[]
            {
                PurchaseStatus.Approved,
                PurchaseStatus.WaitingDelivery,
                PurchaseStatus.DeliveryProblem

            };
            var permissions = new UserAuthorizationPermissions[]
            {
                UserAuthorizationPermissions.Admin
            };
            var result = await CancelPurchase(context, req, status, permissions);
            return _mapper.Map<PurchaseResponse>(result);
        }     

        private Task AddUserInAuthorizeList(Purchase currentPurchase, Context context, CancelPurchaseRequest req)
        {
            var userAuthorizations = new List<PurchaseUserAuthorization>()
                {
                    new PurchaseUserAuthorization()
                    {
                        UserId = context.User.Id,
                        AuthorizationStatus = UserAuthorizationStatus.Reproved,
                        Comment = req.Comment,
                        Permissions = UserAuthorizationPermissions.Admin
                    }
                };
            var authorizationUserGroups = currentPurchase.AuthorizationUserGroups.ToList();
            authorizationUserGroups.Add(new PurchaseAuthUserGroup()
            {
                UserAuthorizations = userAuthorizations
            });
            return Task.CompletedTask;
        }


        private async Task<Purchase> CancelPurchase(Context context, CancelPurchaseRequest req, PurchaseStatus[] status, UserAuthorizationPermissions[] permissions)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                 filter: x => x.Id == req.PurchaseId,
                 include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString());

            var user = await _permissionService.VerifyPermissions(currentPurchase.AuthorizationUserGroups, context, permissions);
            await _permissionService.VerifyStatus(currentPurchase.Status, status);

            if (user == null)
            {
                await AddUserInAuthorizeList(currentPurchase, context, req);
            }
            else
            {
                user.AuthorizationStatus = UserAuthorizationStatus.Reproved;
                user.Comment = req.Comment;
            }
            currentPurchase.Status = PurchaseStatus.Cancelled;
            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return currentPurchase;
        }


        private static PurchaseUserAuthorization UserIsInTheAuthorizationList(Context context, Purchase purchase) 
        {
            var listGroup = purchase.AuthorizationUserGroups.ToList().OrderBy(x => x.Priority);

            foreach (var group in listGroup)
            {
                var userAuth = group.UserAuthorizations.Where(x => x.UserId == context.User.Id).FirstOrDefault(); //TODO: Testa Erro UserID = null
                if (userAuth != null)
                {
                    return userAuth;      
                }

                //Nao pode verificar o proximo grupo se ainda houve pendecias no atual.
                if (group.UserAuthorizations.Any(x => x.AuthorizationStatus == UserAuthorizationStatus.Pending))
                {
                    throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);
                }
            }
            throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);
        }


        private static bool AllMaterialWasDelivered(Purchase purchase, ReceiveDeliveryRequest req)
        {

            int countItemsDelivered = default;

            foreach(var material in purchase.Materials)
            {
                var materialReceive = req.ReceiveDeliveryItemRequests.Where(x => x.MaterialPurchaseId == material.Id).FirstOrDefault();

                var materilAlreadyDelivered = purchase.PurchaseDeliveries.Where(x => x.MaterialPurchaseId == material.Id).ToList();

                var quantityMaterialAlreadyDelivered = materilAlreadyDelivered.Sum(x => x.Quantity); //TODO: Check se a lista for vazia ou nula

                var quantityMaterialNotDelivered = material.Quantity - quantityMaterialAlreadyDelivered;

                if (materialReceive is not null)
                {
                    if(quantityMaterialNotDelivered < materialReceive.Quantity)
                    {
                        throw new BadRequestException(MaterialPurchaseErrors.MaterialReceivedInvalid);
                    }
                    else if (quantityMaterialNotDelivered == materialReceive.Quantity)
                    {
                        countItemsDelivered++;
                    }
                    continue;
                }
                if (quantityMaterialNotDelivered <= 0)
                    countItemsDelivered++;
            }

            if (countItemsDelivered == purchase.Materials.Count())
                return true;

            return false;
        }


    }
}
