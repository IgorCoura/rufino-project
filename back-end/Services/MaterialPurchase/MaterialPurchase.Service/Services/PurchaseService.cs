using AutoMapper;
using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Entities;
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

        public PurchaseService(IPurchaseRepository purchaseRepository, IMapper mapper)
        {
            _purchaseRepository = purchaseRepository;
            _mapper = mapper;
        }

        public async Task<PurchaseResponse> AuthorizePurchase(Context context, PurchaseRequest req)
        {
             var currentPuchase = await _purchaseRepository.FirstAsyncAsTracking(
                 filter: x => x.Id == req.Id, 
                 include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations)) 
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            if (currentPuchase.Status != PurchaseStatus.Authorizing)
                throw new BadRequestException(MaterialPurchaseErrors.PurchaseStatusInvalid, currentPuchase.Status.ToString()); 

            var userAuth = UserIsInTheAuthorizationList(context, currentPuchase);

            userAuth.AuthorizationStatus = UserAuthorizationStatus.Approved;
            await CheckPurchaseAuthorizations(currentPuchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPuchase);
        }

        public async Task<PurchaseResponse> UnlockPurchase(Context context, PurchaseRequest req)
        {
            var currentPuchase = await _purchaseRepository.FirstAsyncAsTracking(
                  filter: x => x.Id == req.Id,
                  include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                 ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            if (currentPuchase.Status != PurchaseStatus.Blocked)
                throw new BadRequestException(MaterialPurchaseErrors.PurchaseStatusInvalid, currentPuchase.Status.ToString()); 

            currentPuchase.Status = PurchaseStatus.Pending;
            await CheckPurchaseAuthorizations(currentPuchase);

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPuchase);
        }

        public async Task<PurchaseResponse> ConfirmDeliveryDate(Context context, ConfirmDeliveryDateRequest req)
        {
            var currentPuchase = await _purchaseRepository.FirstAsyncAsTracking(
                  filter: x => x.Id == req.PurchaseId)
                 ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString());

            if (currentPuchase.Status != PurchaseStatus.Approved || currentPuchase.Status != PurchaseStatus.DeliveryProblem)
                throw new BadRequestException(MaterialPurchaseErrors.PurchaseStatusInvalid, currentPuchase.Status.ToString());

            currentPuchase.Status = PurchaseStatus.WaitingDelivery;
            currentPuchase.LimitDeliveryDate = req.LimitDeliveryDate;

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPuchase);
        }

        public async Task<PurchaseResponse> ReceiveDelivery(Context context, ReceiveDeliveryRequest req)
        {
            var purchase = await _purchaseRepository.FirstAsyncAsTracking(
                  filter: x => x.Id == req.PurchaseId,
                  include: i => i.Include(a => a.PurchaseDeliveries).Include(c => c.Materials))
                 ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString()); 

            var allIsDelivered = CheckIfAllMaterialWasDelivered(purchase, req);

            var newDeliveries = req.ReceiveDeliveryItemRequests.Select(item =>
            {
                return new PurchaseDeliveryItem()
                {
                    ReceiverId = context.User.Id,
                    MaterialPurchaseId = item.MaterialPurchaseId,
                    Quantity = item.Quantity,
                };
            });

            var listDeliveries = purchase.PurchaseDeliveries.ToList();
            listDeliveries.AddRange(newDeliveries);

            if (allIsDelivered)
                purchase.Status = PurchaseStatus.Closed;

            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(purchase);
        }

        public async Task<PurchaseResponse> CancelPurchaseCreator(Context context, CancelPurchaseRequest req)
        {
            var result = await CancelPurchase(context, req, PurchaseStatus.Pending, PurchaseStatus.Authorizing);

            return _mapper.Map<PurchaseResponse>(result);
        }

        public async Task<PurchaseResponse> CancelPurchaseClient(Context context, CancelPurchaseRequest req)
        {
            var result = await CancelPurchase(context, req, PurchaseStatus.Authorizing);

            return _mapper.Map<PurchaseResponse>(result);
        }

        public async Task<PurchaseResponse> CancelPurchaseAdmin(Context context, CancelPurchaseRequest req)
        {
            var currentPuchase = await _purchaseRepository.FirstAsyncAsTracking(
                 filter: x => x.Id == req.PurchaseId,
                 include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString());

            if (currentPuchase.Status != PurchaseStatus.Pending
                || currentPuchase.Status != PurchaseStatus.Blocked
                || currentPuchase.Status != PurchaseStatus.Authorizing
                || currentPuchase.Status != PurchaseStatus.Approved
                || currentPuchase.Status != PurchaseStatus.WaitingDelivery)
            {
                throw new BadRequestException(MaterialPurchaseErrors.PurchaseStatusInvalid, currentPuchase.Status.ToString());
            }
            
            var group = currentPuchase.AuthorizationUserGroups.Where(x => 
                x.UserAuthorizations.Any(u => u.UserId == context.User.Id)).FirstOrDefault() 
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(context.User.Id), context.User.Id.ToString());

            var userAuth = group.UserAuthorizations.Where(x => x.UserId == context.User.Id).FirstOrDefault();

            if(userAuth == null)
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
                var authorizationUserGroups = currentPuchase.AuthorizationUserGroups.ToList();
                authorizationUserGroups.Add(new PurchaseAuthUserGroup()
                {
                    UserAuthorizations = userAuthorizations
                });
            }
            else
            {
                userAuth.AuthorizationStatus = UserAuthorizationStatus.Reproved;
                userAuth.Comment = req.Comment;
            }
            currentPuchase.Status = PurchaseStatus.Cancelled;
            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResponse>(currentPuchase);
        }

        

        public Task CheckPurchaseAuthorizations(Purchase purchase)
        {
            if (purchase.Status != PurchaseStatus.Pending && purchase.Status != PurchaseStatus.Authorizing)
                throw new BadRequestException(MaterialPurchaseErrors.PurchaseStatusInvalid, purchase.Status.ToString());

            var needAuthorization = purchase.AuthorizationUserGroups.Any(x => x.UserAuthorizations.Any(u => u.AuthorizationStatus == UserAuthorizationStatus.Pending));

            if (needAuthorization)
            {
                purchase.Status = PurchaseStatus.Authorizing;
                return Task.CompletedTask;
            }     
            else
            {
                var WasReproved = purchase.AuthorizationUserGroups.Any(x => x.UserAuthorizations.Any(u => u.AuthorizationStatus == UserAuthorizationStatus.Reproved));
                if (WasReproved)
                {
                    purchase.Status = PurchaseStatus.Cancelled;
                    return Task.CompletedTask;
                }                    
            }
            purchase.Status = PurchaseStatus.Approved;
            return Task.CompletedTask;
        }

        private async Task<Purchase> CancelPurchase(Context context, CancelPurchaseRequest req, params PurchaseStatus[] status)
        {
            var currentPuchase = await _purchaseRepository.FirstAsyncAsTracking(
                 filter: x => x.Id == req.PurchaseId,
                 include: i => i.Include(a => a.AuthorizationUserGroups).ThenInclude(b => b.UserAuthorizations))
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.PurchaseId), req.PurchaseId.ToString());

            if (!status.Any(x => x == currentPuchase.Status))
                throw new BadRequestException(MaterialPurchaseErrors.PurchaseStatusInvalid, currentPuchase.Status.ToString());

            var userAuth = UserIsInTheAuthorizationList(context, currentPuchase);

            userAuth.AuthorizationStatus = UserAuthorizationStatus.Reproved;
            userAuth.Comment = req.Comment;
            currentPuchase.Status = PurchaseStatus.Cancelled;
            await _purchaseRepository.UnitOfWork.SaveChangesAsync();

            return currentPuchase;
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


        private static bool CheckIfAllMaterialWasDelivered(Purchase purchase, ReceiveDeliveryRequest req)
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
