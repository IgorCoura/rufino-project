using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Exceptions;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Construction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Services
{
    public class ConstructionService : IConstructionService
    {
        private readonly IConstructionRepository _constructionRepository;
        private readonly IMapper _mapper;

        public ConstructionService(IConstructionRepository constructionRepository, IMapper mapper)
        {
            _constructionRepository = constructionRepository;
            _mapper = mapper;
        }

        public async Task<ConstructionModel> Create(CreateConstructionModel model)
        {


            var entity = new Construction(){
                CorporateName = model.CorporateName,
                NickName = model.NickName,
                Address = new Address(
                    model.Street,
                    model.City,
                    model.State,
                    model.Country,
                    model.ZipCode
                )

            };

            var result = await _constructionRepository.RegisterAsync(entity);
            await _constructionRepository.UnitOfWork.SaveChangesAsync();

            return new ConstructionModel
            (
                result.Id,
                result.CorporateName,
                result.NickName,
                result.Address!.Street,
                result.Address.City,
                result.Address.State,
                result.Address.Country,
                result.Address.ZipCode
            );
        }

    }
}
