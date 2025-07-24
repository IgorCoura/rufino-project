using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;

namespace PeopleManagement.Infra.Mapping
{
    public class DocumentGroupMap : EntityMap<DocumentGroup>
    {
        public override void Configure(EntityTypeBuilder<DocumentGroup> builder)
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

            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
        }
    }
}
