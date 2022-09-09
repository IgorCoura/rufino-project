using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Exceptions;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Brand;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Services
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IMapper _mapper;
        private readonly IValidatorFactory _validatorFactory;
        public BrandService(IBrandRepository brandRepository, IMapper mapper, IValidatorFactory validatorFactory)
        {
            _brandRepository = brandRepository;
            _mapper = mapper;
            _validatorFactory = validatorFactory;
        }

        public async Task<BrandModel> Create(CreateBrandModel model)
        {
            var validation = await _validatorFactory.GetValidator<CreateBrandModel>().ValidateAsync(model);
            if (!validation.IsValid)
                throw new BadRequestException(validation.Errors);


            var entity = _mapper.Map<Brand>(model);
            var result = await _brandRepository.RegisterAsync(entity);
            await _brandRepository.UnitOfWork.SaveChangesAsync();
            return _mapper.Map<BrandModel>(result);
        }
    }
}
