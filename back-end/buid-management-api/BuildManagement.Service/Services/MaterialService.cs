using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Exceptions;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Material;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Services
{
    public class MaterialService : IMaterialService
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly IMapper _mapper;
        private readonly IValidatorFactory _validatorFactory;

        public MaterialService(IMaterialRepository materialRepository, IMapper mapper, IValidatorFactory validatorFactory)
        {
            _materialRepository = materialRepository;
            _mapper = mapper;
            _validatorFactory = validatorFactory;
        }

        public async Task<MaterialModel> Create(CreateMaterialModel model)
        {
            var validator = await _validatorFactory.GetValidator<CreateMaterialModel>().ValidateAsync(model);
            if (!validator.IsValid)
                throw new BadRequestException(validator.Errors);

            var entity = _mapper.Map<Material>(model);
            var result = await _materialRepository.RegisterAsync(entity);
            await _materialRepository.UnitOfWork.SaveChangesAsync();
            return _mapper.Map<MaterialModel>(result);
        }
        
    }
}
