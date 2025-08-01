using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.AggregatesModel.WebHookAggregate;

namespace PeopleManagement.Infra.Mapping
{
    internal class WebHookMap : EntityMap<WebHook>
    {
        public override void Configure(EntityTypeBuilder<WebHook> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.Event).IsUnique();

            builder.Property(x => x.WebHookId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Event)
                    .HasConversion(x => x.Id, x => x)
                    .IsRequired();
        }
    }
            
}
