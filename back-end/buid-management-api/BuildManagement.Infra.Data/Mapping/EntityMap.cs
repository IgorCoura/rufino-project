using BuildManagement.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildManagement.Infra.Data.Mapping
{
    public class EntityMap<T> : IEntityTypeConfiguration<T> where T : Entity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CreateAt).IsRequired();
            builder.Property(x => x.UpdateAt).IsRequired(false);
        }
    }
}
