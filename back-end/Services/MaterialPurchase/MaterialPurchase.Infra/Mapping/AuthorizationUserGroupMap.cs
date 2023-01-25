using Commom.Domain.SeedWork;
using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Infra.Mapping
{
    public class AuthorizationUserGroupMap : EntityMap<AuthorizationUserGroup>
    {
        public override void Configure(EntityTypeBuilder<AuthorizationUserGroup> builder)
        {
            base.Configure(builder);

        }
    }
}
