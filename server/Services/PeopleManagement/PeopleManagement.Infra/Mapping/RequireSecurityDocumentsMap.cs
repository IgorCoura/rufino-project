using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using System.Text.Json;

namespace PeopleManagement.Infra.Mapping
{
    public class RequireSecurityDocumentsMap : EntityMap<RequireSecurityDocuments>
    {
        public override void Configure(EntityTypeBuilder<RequireSecurityDocuments> builder)
        {
            base.Configure(builder);


            builder.Property(x => x.Types)
                        .HasConversion(
                            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                            v => JsonSerializer.Deserialize<List<SecurityDocumentType>>(v, JsonSerializerOptions.Default)!,
                            new ValueComparer<List<SecurityDocumentType>>(
                                (c1, c2) => c1!.SequenceEqual(c2!),
                                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                                c => c.ToList()))
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
