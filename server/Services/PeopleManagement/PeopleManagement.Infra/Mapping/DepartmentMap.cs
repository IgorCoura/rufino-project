using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;

namespace PeopleManagement.Infra.Mapping
{
    public class DepartmentMap : EntityMap<Department>
    {
        public override void Configure(EntityTypeBuilder<Department> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
               .HasConversion(x => x.Value, x => x)
               .HasMaxLength(Name.MAX_LENGTH)
               .IsRequired();

            builder.Property(x => x.Description)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(Description.MAX_LENGTH)
                .IsRequired();
        }
    }
}
