using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using System.Text.Json;

namespace PeopleManagement.Infra.Mapping
{
    public class ArchiveCategoryMap : EntityMap<ArchiveCategory>
    {
        public override void Configure(EntityTypeBuilder<ArchiveCategory> builder)
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

 
            builder.Property(x => x.ListenEventsIds)
                .HasConversion(
                            i => string.Join(",", i),
                            s => string.IsNullOrWhiteSpace(s) ? new List<int>() : s.Split(new[] { ',' }).Select(v => int.Parse(v)).ToList(),
                            new ValueComparer<List<int>>(
                                (c1, c2) => c1!.SequenceEqual(c2!),
                                c => c.GetHashCode(),
                                c => c.ToList()));
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        }

    }
}
