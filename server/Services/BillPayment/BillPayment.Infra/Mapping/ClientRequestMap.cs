namespace BillPayment.Infra.Mapping;

using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ClientRequestMap : IEntityTypeConfiguration<ClientRequest>
{
    public void Configure(EntityTypeBuilder<ClientRequest> builder)
    {
        builder.ToTable("client_requests");

        // Chave composta: a marca é por tenant E por comando (2026-08-28). Só pelo id, um
        // x-requestid de um tenant valia para todos, e o mesmo id em outro comando era engolido.
        // As linhas anteriores à migração ficam com tenant_id zerado e nunca colidem com ninguém.
        builder.HasKey(e => new { e.TenantId, e.Id, e.Name });
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Time).HasColumnName("time").IsRequired();
    }
}
