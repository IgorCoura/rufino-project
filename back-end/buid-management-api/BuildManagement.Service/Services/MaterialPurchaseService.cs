using AutoMapper;
using BuildManagement.Domain.Entities.Enum;
using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.Exceptions;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using BuildManagement.Domain.Models.Purchase.MaterialReceive;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider _serviceProvider;

        public MaterialPurchaseService(IMaterialPurchaseRepository materialPurchaseRepository, IMapper mapper, IServiceProvider serviceProvider)
        {
            _materialPurchaseRepository = materialPurchaseRepository;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        public async Task<CreateMaterialPurchaseResponse> CreateMaterialPurchase(CreateMaterialPurchaseRequest model)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<CreateMaterialPurchaseRequest>>().ValidateAsync(model);
            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = _mapper.Map<MaterialPurchase>(model);
            entity.Status = MaterialPurchaseStatus.Pending;
            entity.Materials.ForEach(x => x.QuantityNotReceived = x.Quantity);
            var result = await _materialPurchaseRepository.RegisterAsync(entity);
            await _materialPurchaseRepository.UnitOfWork.SaveChangesAsync();
            return _mapper.Map<CreateMaterialPurchaseResponse>(result);
        }

        public async Task PreAuthorization(Guid id)
        {
            //TODO: Gerar um pdf com a autorização

            var entity = await _materialPurchaseRepository.FirstAsyncAsTracking(x => x.Id == id)
                ?? throw new BadRequestException(ErrorsMessages.NotFoundMaterialPurchase, id.ToString());

            if (entity.Status != MaterialPurchaseStatus.Pending)
                throw new BadRequestException(ErrorsMessages.StatusChangeInvalid, entity.Status.ToString(), MaterialPurchaseStatus.Authorized.ToString());


            entity.Status = MaterialPurchaseStatus.PreAuthorized;
            await _materialPurchaseRepository.UnitOfWork.SaveChangesAsync();
        }

        public async Task Authorization(Guid id)
        {
            //TODO: Receber comprovante de autorização
            var entity = await _materialPurchaseRepository.FirstAsyncAsTracking(x => x.Id == id, include: x => x.Include(m => m.Materials))
                ?? throw new BadRequestException(ErrorsMessages.NotFoundMaterialPurchase, id.ToString());

            if (entity.Status != MaterialPurchaseStatus.PreAuthorized)
                throw new BadRequestException(ErrorsMessages.StatusChangeInvalid, entity.Status.ToString(), MaterialPurchaseStatus.Authorized.ToString());

            entity.Status = MaterialPurchaseStatus.Authorized;
            entity.Materials.ForEach(x => x.Status = MaterialStatus.NotReceived);
            await _materialPurchaseRepository.UnitOfWork.SaveChangesAsync();
        }

        public async Task<MaterialReceiveResponse> MaterialReceive(MaterialReceiveRequest model)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<MaterialReceiveRequest>>().ValidateAsync(model);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = await _materialPurchaseRepository.FirstAsyncAsTracking(x => x.Id == model.MaterialPurchaseId, include: x => x.Include(m => m.Materials))
                ?? throw new BadRequestException(ErrorsMessages.NotFoundMaterialPurchase, model.MaterialPurchaseId.ToString());

            if (entity.Status != MaterialPurchaseStatus.Authorized && entity.Status != MaterialPurchaseStatus.PartialReceived)
                throw new BadRequestException(ErrorsMessages.InvalidStatus, entity.Status.ToString());

            await UpdateStatusAllItensMaterials(model, entity);

            if (entity.Materials.Any(x => x.Status == MaterialStatus.NotReceived))
            {
                entity.Status = MaterialPurchaseStatus.PartialReceived;
            }

            if (!entity.Materials.Any(x => x.Status == MaterialStatus.NotReceived))
            {
                entity.Status = MaterialPurchaseStatus.Received;
                entity.Materials.ForEach(x => x.Status = MaterialStatus.Closed);
            }

            await _materialPurchaseRepository.UnitOfWork.SaveChangesAsync();
            return _mapper.Map<MaterialReceiveResponse>(entity);
        }


        public async Task<ReturnMaterialPurchaseModel> Get(Guid id)
        {
            var entity = await _materialPurchaseRepository.FirstAsync(x => x.Id == id, include: x => x.Include(m => m.Materials));
            return _mapper.Map<ReturnMaterialPurchaseModel>(entity);
        }

        private async Task UpdateStatusAllItensMaterials(MaterialReceiveRequest model, MaterialPurchase entity)
        {
            var errors = new BadRequestException();
            var tasks = new List<Task>();
            for(int i = 0; i < entity.Materials.Count; i++)
            {
                tasks.Add(UpdateStatusItemMaterial(i, model.MaterialReceive[i], entity, errors));
            }

            await Task.WhenAll(tasks);

            if (errors.HasErrors())
            {
                throw errors;
            }
        }

        private Task UpdateStatusItemMaterial(int index, ItemMaterialReceiveRequest model, MaterialPurchase entity, BadRequestException errors)
        {
            var itemMaterial = entity.Materials.Where(x => x.Id == model.ItemMaterialId).FirstOrDefault();
            if (itemMaterial is not null)
            {
                itemMaterial.QuantityNotReceived = itemMaterial.QuantityNotReceived - model.QuantityReceived;
                if (itemMaterial.QuantityNotReceived > 0)
                {
                    itemMaterial.Status = MaterialStatus.NotReceived;
                }
                else if (itemMaterial.QuantityNotReceived == 0)
                {
                    itemMaterial.Status = MaterialStatus.Received;
                }
                else
                {
                    errors.AddError(ErrorsMessages.ExcessiveAmountMaterial, index.ToString());
                }

            }
            else
            {
                errors.AddError(ErrorsMessages.MaterialNotFound, model.ItemMaterialId.ToString());
            }
            return Task.CompletedTask;
        }

    }
}
