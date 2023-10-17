using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Commom.Domain.BaseEntities;
using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.MessageBroker.Bus;
using Commom.MessageBroker.Message.MaterialControlMessages;
using EasyNetQ;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using MaterialControl.Domain.Entities;
using MaterialControl.Domain.Interfaces;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaterialControl.Services.Services.Modify
{
    public class MaterialService : IMaterialService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMapper _mapper;
        private readonly IMaterialRepository _materialRepository;
        private readonly IPublishSubscribe _pubSub;

        public MaterialService(IServiceProvider serviceProvider, IMapper mapper, IMaterialRepository materialRepository, IPublishSubscribe pubSub)
        {
            _serviceProvider = serviceProvider;
            _mapper = mapper;
            _materialRepository = materialRepository;
            _pubSub = pubSub;
        }

        public async Task<MaterialResponse> Create(Context context, CreateMaterialRequest req)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<CreateMaterialRequest>>().ValidateAsync(req);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = _mapper.Map<Material>(req);
            try
            {
                var result = await _materialRepository.RegisterAsync(entity);
                entity = await _materialRepository.FirstAsync(x => x.Id == result.Id, include: i => i.Include(o => o.Unity)!);
                await _pubSub.PublishMessageAsync(_mapper.Map<ModifyMaterialMessage>(entity));
                await _materialRepository.UnitOfWork.SaveChangesAsync();
                return _mapper.Map<MaterialResponse>(result);
            }
            catch (ReferenceConstraintException)
            {
                throw new BadRequestException(CommomErrors.ReferenceConstraintViolation);
            }
            catch (UniqueConstraintException)
            {
                throw new BadRequestException(CommomErrors.UniqueConstraintViolation);
            };

        }

        public async Task<MaterialResponse> Update(Context context, MaterialRequest req)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<MaterialRequest>>().ValidateAsync(req);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = await _materialRepository.FirstAsyncAsTracking(x => x.Id == req.Id, include: i => i.Include(o => o.Unity)!)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            entity.Name = req.Name;
            entity.Description = req.Description;
            entity.UnityId = req.UnityId;

            try
            {
                await _pubSub.PublishMessageAsync(_mapper.Map<ModifyMaterialMessage>(entity));
                await _materialRepository.UnitOfWork.SaveChangesAsync();
                return _mapper.Map<MaterialResponse>(entity);
            }
            catch (ReferenceConstraintException)
            {
                throw new BadRequestException(CommomErrors.ReferenceConstraintViolation);
            }
            catch (UniqueConstraintException)
            {
                throw new BadRequestException(CommomErrors.UniqueConstraintViolation);
            };
        }

        public async Task Delete(Context context, Guid id)
        {
            var entity = await _materialRepository.FirstAsyncAsTracking(x => x.Id == id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            await _materialRepository.DeleteAsync(entity);

            await _pubSub.PublishMessageAsync(_mapper.Map<DeleteMaterialMessage>(entity));

            await _materialRepository.UnitOfWork.SaveChangesAsync();
        }

        public async Task<MaterialResponse> Recover(Guid id)
        {
            var entity = await _materialRepository.FirstAsyncAsTracking(x => x.Id == id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            return _mapper.Map<MaterialResponse>(entity);
        }

        public async Task<IEnumerable<MaterialResponse>> RecoverAll()
        {
            var entities = await _materialRepository.GetDataAsync();
            return _mapper.Map<IEnumerable<MaterialResponse>>(entities);
        }
    }
}
