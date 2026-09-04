namespace BillPayment.Application.Mediator;

/// <summary>
/// Todo Command deste BC nasce com o <c>TenantId</c> como primeiro parâmetro; este marcador é o
/// que deixa o pipeline LER esse tenant sem conhecer o tipo concreto.
/// </summary>
/// <remarks>
/// Quem depende dele é a idempotência: a marca de <c>x-requestid</c> é por
/// <c>(tenant, id, comando)</c> desde 2026-08-28. Antes era só pelo id — um id repetido por
/// outro tenant (ou por outro comando do mesmo tenant) devolvia resposta neutra e o comando era
/// engolido em silêncio. Records posicionais com <c>Guid TenantId</c> implementam a propriedade
/// sozinhos; o construtor genérico do <c>IdentifiedCommandHandler</c> recusa quem não a tiver.
/// </remarks>
public interface ITenantScopedCommand
{
    Guid TenantId { get; }
}
