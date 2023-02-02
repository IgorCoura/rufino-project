using Commom.Domain.BaseEntities;
using Commom.Infra.Base;
using Commom.Infra.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Infra.Repository
{
    public class RoleRepository<context> : BaseRepository<Role>, IRoleRepository where context : BaseContext
    {
        public RoleRepository(context context) : base(context)
        {
        }
    }
}
