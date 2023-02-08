using Commom.Infra.Base;
using MaterialControl.Domain.Entities;
using MaterialControl.Domain.Interfaces;
using MaterialControl.Infra.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Infra.Repository
{
    public class UnityRepository : BaseRepository<Unity>, IUnityRepository
    {
        public UnityRepository(MaterialControlContext context) : base(context)
        {
        }
    }
}
