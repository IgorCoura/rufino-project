using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Infra.Mapping
{
    public class DocumentTemplateMap : EntityMap<DocumentTemplate>
    {
        public override void Configure(EntityTypeBuilder<DocumentTemplate> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Directory)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(DirectoryName.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.BodyFileName)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(FileName.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.HeaderFileName)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(FileName.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.FooterFileName)
                .HasConversion(x => x.Value, x => x)
                .HasMaxLength(FileName.MAX_LENGTH)
                .IsRequired();

            builder.Property(x => x.RecoverDataType)
                .HasConversion(x => x.Id, x => x)
                .IsRequired();

            builder.Property(x => x.DocumentValidityDuration)
                .IsRequired();

        }
    }
}
