using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Infra.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Repository
{
    public class MaterialRepository : BaseRepository<Material>, IMaterialRepository
    {
        public MaterialRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
