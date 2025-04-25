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

            builder.HasIndex(x => x.AssociationId);
                

            builder.Property(x => x.AssociationId)
                .IsRequired();

            builder.Property(x => x.AssociationType)
                .HasConversion(x => x.Id, x => x)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(Name.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(Description.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.ListenEventsIds)
                .HasConversion(
                            i => string.Join(",", i),
                            s => string.IsNullOrWhiteSpace(s) ? new List<int>() : s.Split(new[] { ',' }).Select(v => int.Parse(v)).ToList(),
                            new ValueComparer<List<int>>(
                                (c1, c2) => c1!.SequenceEqual(c2!),
                                c => c.GetHashCode(),
                                c => c.ToList()));

            builder.HasMany<DocumentTemplate>()
              .WithMany();

            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

         
        }
    }
}
