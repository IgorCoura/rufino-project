using Commom.Infra.Base;
using MaterialControl.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaterialControl.Infra.Mapping
{
    public class MaterialMap : EntityMap<Material>
    {
        public override void Configure(EntityTypeBuilder<Material> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.Name)
               .IsUnique();

            builder.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(250)
                .IsRequired();

            builder.HasOne(x => x.Unity)
                .WithMany()
                .HasForeignKey(x => x.UnityId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        }
    }
}
