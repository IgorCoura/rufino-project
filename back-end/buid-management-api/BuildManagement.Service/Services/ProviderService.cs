using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Provider;


namespace BuildManagement.Service.Services
{
    public class ProviderService : IProviderService
    {
        private readonly IProviderRepository _providerRepository;
        private readonly IMapper _mapper;
        public ProviderService(IProviderRepository providerRepository, IMapper mapper)
        {
            _providerRepository = providerRepository;
            _mapper = mapper;
        }

        public async Task<ProviderModel> Create(CreateProviderModel model)
        {
            
            var entity = _mapper.Map<Provider>(model);
            var result = await _providerRepository.RegisterAsync(entity);
            await _providerRepository.UnitOfWork.SaveEntitiesAsync();
            return _mapper.Map<ProviderModel>(result);           
        } 


    }
}
