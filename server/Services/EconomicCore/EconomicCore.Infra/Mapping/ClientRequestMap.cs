namespace EconomicCore.Infra.Mapping;

using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ClientRequestMap : IEntityTypeConfiguration<ClientRequest>
{
    public void Configure(EntityTypeBuilder<ClientRequest> builder)
    {
        builder.ToTable("client_requests");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Time).HasColumnName("time").IsRequired();
    }
}
