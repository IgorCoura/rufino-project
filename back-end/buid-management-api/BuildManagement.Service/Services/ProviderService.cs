using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Exceptions;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Provider;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace BuildManagement.Service.Services
{
    public class ProviderService : IProviderService
    {
        private readonly IProviderRepository _providerRepository;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;
        public ProviderService(IProviderRepository providerRepository, IMapper mapper, IServiceProvider serviceProvider)
        {
            _providerRepository = providerRepository;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        public async Task<ProviderModel> Create(CreateProviderModel model)
        {
            var validator = await _serviceProvider.GetRequiredService<IValidator<CreateProviderModel>>().ValidateAsync(model);

            if (!validator.IsValid)
                throw new BadRequestException(validator.Errors);

            var entity = new Provider()
            {
                Name = model.Name,
                Description = model.Description,
                Cnpj = model.Cnpj,
                Email = model.Email,
                Site = model.Site,
                Phone = model.Phone,
                Address = new Address(
                    model.Street,
                    model.City,
                    model.State,
                    model.Country,
                    model.ZipCode
                    )
            };


            var result = await _providerRepository.RegisterAsync(entity);
            await _providerRepository.UnitOfWork.SaveChangesAsync();

            return new ProviderModel
            (
                result.Id,
                result.Name,
                result.Description,
                result.Cnpj,
                result.Email,
                result.Site,
                result.Phone,
                result.Address!.Street,
                result.Address.City,
                result.Address.State,
                result.Address.Country,
                result.Address.ZipCode
            );


            
        }

        


    }
}
