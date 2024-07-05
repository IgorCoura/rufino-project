using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;

namespace PeopleManagement.Infra.Mapping
{
    public class SecurityDocumentMap : EntityMap<SecurityDocument>
    {
        public override void Configure(EntityTypeBuilder<SecurityDocument> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Status)
                .HasConversion(x => x.Id, x => x)
                .IsRequired();

            builder.Property(x => x.Type)
                .HasConversion(x => x.Id, x => x)
                .IsRequired();

            
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasMany(x => x.Documents)
                .WithOne(x => x.SecurityDocument)
                .HasForeignKey(x => x.SecurityDocumentId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}
