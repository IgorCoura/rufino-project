using AutoMapper;
using Commom.Domain.BaseEntities;
using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using FluentValidation;
using MaterialControl.Domain.Entities;
using MaterialControl.Domain.Interfaces;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;
using Microsoft.Extensions.DependencyInjection;

namespace MaterialControl.Services.Services
{
    public class BrandService : IBrandService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBrandRepository _brandRepository;
        private readonly IMapper _mapper;

        public BrandService(IServiceProvider serviceProvider, IBrandRepository brandRepository, IMapper mapper)
        {
            _serviceProvider = serviceProvider;
            _brandRepository = brandRepository;
            _mapper = mapper;
        }

        public async Task<BrandResponse> Create(Context context, CreateBrandRequest req)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<CreateBrandRequest>>().ValidateAsync(req);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = _mapper.Map<Brand>(req);

            var result = await _brandRepository.RegisterAsync(entity);
            await _brandRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<BrandResponse>(result);
        }


        public async Task<BrandResponse> Update(Context context, BrandRequest req)
        {
            var validation = await _serviceProvider.GetRequiredService<IValidator<BrandRequest>>().ValidateAsync(req);

            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);

            var entity = await _brandRepository.FirstAsyncAsTracking(x => x.Id == req.Id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(req.Id), req.Id.ToString());

            entity.Name = req.Name;
            entity.Description = req.Description;
            await _brandRepository.UnitOfWork.SaveChangesAsync();

            return _mapper.Map<BrandResponse>(entity);
        }

        public async Task Delete(Context context, Guid id)
        {
            var entity = await _brandRepository.FirstAsync(x => x.Id == id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            await _brandRepository.DeleteAsync(entity);
            await _brandRepository.UnitOfWork.SaveChangesAsync();
        }

        public async Task<BrandResponse> Recover(Guid id)
        {
            var entity = await _brandRepository.FirstAsync(x => x.Id == id)
                ?? throw new BadRequestException(CommomErrors.PropertyNotFound, nameof(id), id.ToString());

            return _mapper.Map<BrandResponse>(entity);
        }

        public async Task<IEnumerable<BrandResponse>> RecoverAll()
        {
            var entities = await _brandRepository.GetDataAsync();
            return _mapper.Map<IEnumerable<BrandResponse>>(entities);
        }
    }
}
