using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;

namespace PeopleManagement.Infra.Mapping
{
    public class ArchiveMap : EntityMap<Archive>
    {
        public override void Configure(EntityTypeBuilder<Archive> builder)
        {
            base.Configure(builder);

            builder.OwnsMany(x => x.Files, file =>
            {
                file.HasKey("Id");

                file.Property(x => x.Name)
                    .HasConversion(x => x.Value, x => x)
                    .HasMaxLength(Name.MAX_LENGTH)
                    .IsRequired();

                file.Property(x => x.Extension)
                    .HasConversion(x => x.Id, x => x)
                    .IsRequired(); 

                file.Property(x => x.Status)
                    .HasConversion(x => x.Id, x => x)
                    .IsRequired();

                file.Property(x => x.InsertAt)
                    .IsRequired();
            });

            builder.Property(x => x.Category)
                .HasConversion(x => x.Id, x => x)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion(x => x.Id, x => x)
                .IsRequired();


        }
    }
}
