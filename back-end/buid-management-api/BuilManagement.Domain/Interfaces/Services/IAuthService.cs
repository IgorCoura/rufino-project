using BuildManagement.Domain.Models.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IAuthService
    {
        Task<string> Login(LoginModel model);
    }
}
