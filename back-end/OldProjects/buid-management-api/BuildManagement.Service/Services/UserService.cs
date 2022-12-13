using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.User;
using BuildManagement.Service.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<UserModel> Create(CreateUserModel model)
        {
            var entity = _mapper.Map<User>(model);
            entity.Password = PasswordHasher.Hash(model.Password);
            var result = await _userRepository.RegisterAsync(entity);
            await _userRepository.UnitOfWork.SaveChangesAsync();
            return _mapper.Map<UserModel>(result);
        }
    }
}
