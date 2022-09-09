using BuildManagement.Domain.Models.Material;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IMaterialService
    {
        Task<MaterialModel> Create(CreateMaterialModel model);
    }
}
