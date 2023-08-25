using AutoMapper;
using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using MaterialPurchase.Domain.Errors;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using Microsoft.EntityFrameworkCore;
using MaterialPurchase.Domain.Interfaces.Repositories;
using MaterialPurchase.Domain.Interfaces.Services;

namespace MaterialPurchase.Service.Services.Modify
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

        public async Task<PurchaseResponse> AuthorizePurchase(Context context, ReleasePurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                filter: x => x.Id == req.Id,
                include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
               ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.Authorizing);

            var userAuth = await _permissionService.FindUserAuthorization(context.User.Id, currentPurchase.AuthorizationUserGroups)
                ?? throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);

            userAuth.AuthorizationStatus = req.Approve ? UserAuthorizationStatus.Approved : UserAuthorizationStatus.Reproved;
            userAuth.Comment = req.Comment;

            await _permissionService.CheckPurchaseAuthorizations(currentPurchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPurchase);
        }

        public async Task<PurchaseResponse> UnlockPurchase(Context context, ReleasePurchaseRequest req)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                  filter: x => x.Id == req.Id,
                  include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                 ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            await _permissionService.VerifyStatus(currentPurchase.Status, PurchaseStatus.Blocked);

            if (req.Approve)
            {
                currentPurchase.Status = PurchaseStatus.Pending;
            }
            else
            {
                var user = await _permissionService.FindUserAuthorization(context.User.Id, currentPurchase.AuthorizationUserGroups)
                    ?? throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);

                if (user == null)
                    user = await AddUserInAuthorizeList(currentPurchase, context.User.Id);    
                
                user.AuthorizationStatus = UserAuthorizationStatus.Reproved;
                user.Comment = req.Comment;
                currentPurchase.Status = PurchaseStatus.Authorizing;
            }
            
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
                  include: i => i.Include(a => a.PurchaseDeliveries).Include(c => c.Materials)
                                .Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                 ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString());

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
            else
                currentPurchase.Status = PurchaseStatus.DeliveryProblem;

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPurchase);
        }


        public async Task<PurchaseResponse> CancelPurchaseBeforeAuthorize(Context context, CancelPurchaseRequest req)
        {
            var status = new PurchaseStatus[]
            {
                PurchaseStatus.Pending
            };

            var result = await CancelPurchase(context, req, status);
            return _mapper.Map<PurchaseResponse>(result);
        }

        public async Task<PurchaseResponse> CancelPurchaseAdmin(Context context, CancelPurchaseRequest req)
        {
            var status = new PurchaseStatus[]
            {
                PurchaseStatus.Pending,
                PurchaseStatus.Authorizing,
                PurchaseStatus.Approved,
                PurchaseStatus.WaitingDelivery,
                PurchaseStatus.DeliveryProblem

            };
            var result = await CancelPurchase(context, req, status);
            return _mapper.Map<PurchaseResponse>(result);
        }

        private async Task<Purchase> CancelPurchase(Context context, CancelPurchaseRequest req, PurchaseStatus[] status)
        {
            var currentPurchase = await _purchaseRepository.FirstAsyncAsTracking(
                 filter: x => x.Id == req.PurchaseId,
                 include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString());
            
            await _permissionService.VerifyStatus(currentPurchase.Status, status);

            var user = await _permissionService.FindUserAuthorization(context.User.Id, currentPurchase.AuthorizationUserGroups)
                ?? throw new BadRequestException(MaterialPurchaseErrors.AuthorizationInvalid);

            if (user == null)
            {
                user = await AddUserInAuthorizeList(currentPurchase, context.User.Id);
                user.AuthorizationStatus = UserAuthorizationStatus.Reproved;
                user.Comment = req.Comment;
            }
            currentPurchase.Status = PurchaseStatus.Cancelled;
            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return currentPurchase;
        }

        private static Task<PurchaseUserAuthorization> AddUserInAuthorizeList(Purchase currentPurchase, Guid id)
        {
            var userAuthorization = new PurchaseUserAuthorization()
            {
                UserId = id,
                AuthorizationStatus = UserAuthorizationStatus.Pending,
                Comment = ""
            };
            var userAuthorizations = new List<PurchaseUserAuthorization>() 
            {
                userAuthorization
            };
            var authorizationUserGroups = currentPurchase.AuthorizationUserGroups.ToList();
            authorizationUserGroups.Add(new PurchaseAuthUserGroup()
            {
                UserAuthorizations = userAuthorizations
            });
            return Task.FromResult(userAuthorization);
        }

        private static bool AllMaterialWasDelivered(Purchase purchase, ReceiveDeliveryRequest req)
        {

            int countItemsDelivered = default;

            foreach (var material in purchase.Materials)
            {
                var materialReceive = req.ReceiveDeliveryItemRequests.Where(x => x.MaterialPurchaseId == material.Id).FirstOrDefault();

                var materilAlreadyDelivered = purchase.PurchaseDeliveries.Where(x => x.MaterialPurchaseId == material.Id).ToList();

                var quantityMaterialAlreadyDelivered = materilAlreadyDelivered.Sum(x => x.Quantity);

                var quantityMaterialNotDelivered = material.Quantity - quantityMaterialAlreadyDelivered;

                if (materialReceive == null)
                {
                    throw new BadRequestException(MaterialPurchaseErrors.MaterialReceivedInvalid);
                }

                if (quantityMaterialNotDelivered < materialReceive.Quantity)
                {
                    throw new BadRequestException(MaterialPurchaseErrors.MaterialReceivedInvalid);
                }
                else if (quantityMaterialNotDelivered == materialReceive.Quantity)
                {
                    countItemsDelivered++;
                }
                else if (quantityMaterialNotDelivered <= 0)
                {
                    countItemsDelivered++;
                }

            }

            if (countItemsDelivered == purchase.Materials.Count())
                return true;

            return false;
        }


    }
}
