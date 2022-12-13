using BuildManagement.Domain.Models.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IProviderService
    {
        Task<ProviderModel> Create(CreateProviderModel model);
    }
}
