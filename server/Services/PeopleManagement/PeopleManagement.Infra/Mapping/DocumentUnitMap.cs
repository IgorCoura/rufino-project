using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DocumentUnit = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.DocumentUnit;
using Name = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Name;

namespace PeopleManagement.Infra.Mapping
{
    public class DocumentUnitMap : EntityMap<DocumentUnit>
    {
        public override void Configure(EntityTypeBuilder<DocumentUnit> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.Validity)
                .IsRequired(false);

            builder.Property(x => x.Name)
                .HasConversion(x => x!.Value, x => x)
                .HasMaxLength(Name.MAX_LENGTH)
                .IsRequired(false);

            builder.Property(x => x.Extension)
                .HasConversion(x => x!.Id, x => x)
                .IsRequired(false);

            builder.Property(x => x.Status)
                .HasConversion(x => x.Id, x => x)
                .IsRequired();

            builder.Property(x => x.Date)
                .IsRequired();

            builder.Property(x => x.SignatureDocumentToken)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(x => x.SignatureUrl)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.OwnsOne(x => x.Period, period =>
            {
                period.Property(c => c.Type)
                    .HasConversion(x => x.Id, x => x)
                    .IsRequired(false);

                period.Property(c => c.Year)
                    .IsRequired();

                period.Property(c => c.Month)
                    .IsRequired();

                period.Property(c => c.Day)
                    .IsRequired(false);

                period.Property(c => c.Week)
                    .IsRequired(false);
            });




        }
    }
}
