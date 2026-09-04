namespace BillPayment.Domain.Bills;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;

/// <summary>
/// Uma tentativa de consulta oficial, guardada para a auditoria.
/// </summary>
/// <remarks>
/// <para>
/// O retrato envelhece — valor de boleto vencido muda todo dia —, então revalidar
/// <strong>substitui</strong> o snapshot corrente. O que não pode acontecer é o anterior sumir
/// em silêncio: meses depois é preciso responder "com que informação essa aprovação foi dada".
/// Cada tentativa entra aqui, inclusive as que não resolveram: saber que a consulta ficou
/// indisponível às 14h é parte da história.
/// </para>
/// <para>
/// <strong>A garantia de só-append é invariante de domínio, não de armazenamento.</strong> A
/// coleção é gravada como uma coluna <c>jsonb</c> no próprio boleto (os retratos contêm
/// <c>Money</c> e <c>TaxId</c> aninhados, e owned type de 2º nível em agregado já persistido
/// grava NULL — a lição do EconomicCore). Nenhum método remove item; promover para tabela
/// filha append-only é o passo seguinte se a auditoria exigir a garantia no banco.
/// </para>
/// </remarks>
public sealed class BillLookupRecord : ValueObject
{
    public PaymentRail Rail { get; private set; } = default!;
    public LookupStatus Status { get; private set; } = default!;

    /// <summary>Preenchido só quando a consulta resolveu.</summary>
    public LookupSnapshot? BankSlipSnapshot { get; private set; }

    /// <summary>Preenchido só quando o decode do Pix resolveu.</summary>
    public PixLookupSnapshot? PixSnapshot { get; private set; }

    public string? ReasonCode { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }

    private BillLookupRecord() { }

    /// <summary>
    /// Também é o caminho de rehidratação: a Infra remonta o <see cref="BillLookupResult"/> a
    /// partir das colunas e chama aqui, em vez de existir uma factory própria de persistência
    /// capaz de fabricar histórico que a consulta nunca devolveu.
    /// </summary>
    public static BillLookupRecord ForBankSlip(BillLookupResult result)
        => new()
        {
            Rail = PaymentRail.Boleto,
            Status = result.Status,
            BankSlipSnapshot = result.Snapshot,
            ReasonCode = result.ReasonCode,
            AttemptedAt = result.AttemptedAt,
        };

    /// <inheritdoc cref="ForBankSlip"/>
    public static BillLookupRecord ForPix(PixLookupResult result)
        => new()
        {
            Rail = PaymentRail.Pix,
            Status = result.Status,
            PixSnapshot = result.Snapshot,
            ReasonCode = result.ReasonCode,
            AttemptedAt = result.AttemptedAt,
        };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Rail;
        yield return Status;
        yield return BankSlipSnapshot;
        yield return PixSnapshot;
        yield return ReasonCode;
        yield return AttemptedAt;
    }
}
