using BuildManagement.Domain.Models.Brand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Interfaces.Services
{
    public interface IBrandService
    {
        Task<BrandModel> Create(CreateBrandModel model);
    }
}
