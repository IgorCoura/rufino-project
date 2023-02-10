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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Services.Services
{
    public class UnityService : IUnityService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMapper _mapper;
        private readonly IUnityRepository _unityRepository;

        public UnityService(IServiceProvider serviceProvider, IMapper mapper, IUnityRepository unityRepository)
        {
            _serviceProvider = serviceProvider;
            _mapper = mapper;
            _unityRepository = unityRepository;
        }

        public async Task<UnityResponse> Create(Context context, CreateUnityRequest req)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<CreateUnityRequest>>().ValidateAsync(req);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = _mapper.Map<Unity>(req);
            try
            {
                var result = await _unityRepository.RegisterAsync(entity);
                await _unityRepository.UnitOfWork.SaveChangesAsync();

                return _mapper.Map<UnityResponse>(result);
            }
            catch (UniqueConstraintException)
            {
                throw new BadRequestException(CommomErrors.UniqueConstraintViolation);
            }
            
        }        

        public async Task<UnityResponse> Update(Context context, UnityRequest req)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<UnityRequest>>().ValidateAsync(req);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = await _unityRepository.FirstAsyncAsTracking(x => x.Id == req.Id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            entity.Name = req.Name;

            try
            {
                await _unityRepository.UnitOfWork.SaveChangesAsync();

                return _mapper.Map<UnityResponse>(entity);
            }
            catch (UniqueConstraintException)
            {
                throw new BadRequestException(CommomErrors.UniqueConstraintViolation);
            }
        }

        public async Task Delete(Context context, Guid id)
        {
            var entity = await _unityRepository.FirstAsync(x => x.Id == id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            await _unityRepository.DeleteAsync(entity);
            await _unityRepository.UnitOfWork.SaveChangesAsync();            
        }

        public async Task<UnityResponse> Recover(Guid id)
        {
            var entity = await _unityRepository.FirstAsync(x => x.Id == id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            return _mapper.Map<UnityResponse>(entity);
        }

        public async Task<IEnumerable<UnityResponse>> RecoverAll()
        {
            var entities = await _unityRepository.GetDataAsync();

            return _mapper.Map<IEnumerable<UnityResponse>>(entities);
        }
    }
}
