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
using EntityFramework.Exceptions.Common;
using FluentValidation;
using MaterialControl.Domain.Entities;
using MaterialControl.Domain.Interfaces;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;
using Microsoft.Extensions.DependencyInjection;

namespace MaterialControl.Services.Services
{
    public class MaterialService : IMaterialService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMapper _mapper;
        private readonly IMaterialRepository _materialRepository;

        public MaterialService(IServiceProvider serviceProvider, IMapper mapper, IMaterialRepository materialRepository)
        {
            _serviceProvider = serviceProvider;
            _mapper = mapper;
            _materialRepository = materialRepository;
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
                await _materialRepository.UnitOfWork.SaveChangesAsync();
                return _mapper.Map<MaterialResponse>(result);
            }
            catch (ReferenceConstraintException)
            {
                throw new BadRequestException(CommomErrors.ReferenceConstraintViolation);
            };

        }       

        public async Task<MaterialResponse> Update(Context context, MaterialRequest req)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<MaterialRequest>>().ValidateAsync(req);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = await _materialRepository.FirstAsyncAsTracking(x => x.Id == req.Id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            entity.Name = req.Name;
            entity.Description = req.Description;
            entity.UnityId = req.UnityId;

            try
            {
                await _materialRepository.UnitOfWork.SaveChangesAsync();
                return _mapper.Map<MaterialResponse>(entity);
            }
            catch (ReferenceConstraintException)
            {
                throw new BadRequestException(CommomErrors.ReferenceConstraintViolation);
            };
        }

        public async Task Delete(Context context, Guid id)
        {
            var entity = await _materialRepository.FirstAsyncAsTracking(x => x.Id == id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            await _materialRepository.DeleteAsync(entity);
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
