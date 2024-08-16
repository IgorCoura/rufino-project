using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using System.Text.Json;
using Name = PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Name;
using Description = PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Description;

namespace PeopleManagement.Infra.Mapping
{
    public class RequireDocumentsMap : EntityMap<RequireDocuments>
    {
        public override void Configure(EntityTypeBuilder<RequireDocuments> builder)
        {
            base.Configure(builder);

            builder.HasMany<DocumentTemplate>()
                .WithMany();

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

            builder.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
        }
    }
}
