using BuildManagement.Domain.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IUserService
    {
        Task<UserModel> Create(CreateUserModel model);
    }
}
