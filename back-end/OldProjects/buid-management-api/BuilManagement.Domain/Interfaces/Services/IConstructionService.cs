using BuildManagement.Domain.Models.Construction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IConstructionService
    {
        Task<ConstructionModel> Create(CreateConstructionModel model);
    }
}
