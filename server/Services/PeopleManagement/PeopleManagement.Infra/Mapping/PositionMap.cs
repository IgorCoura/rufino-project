using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using Department = PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Department;

namespace PeopleManagement.Infra.Mapping
{
    public class PositionMap : EntityMap<Position>
    {
        public override void Configure(EntityTypeBuilder<Position> builder)
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

            builder.Property(x => x.CBO)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(CBO.MAX_LENGTH)
                .IsRequired();

            builder.HasOne<Department>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .IsRequired();
        }
    }
}
