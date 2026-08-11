namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que a busca global por duplicata pode contar sem violar o isolamento entre contas.
/// </summary>
/// <remarks>
/// <see cref="OriginalBillId"/> é preenchido <strong>apenas</strong> quando o boleto original
/// pertence ao tenant que perguntou. Quando é de outro, resta o <see cref="Exists"/> — e a
/// evidência que chega ao usuário é o aviso genérico "já está sob gestão de outra conta do
/// sistema", sem id, sem nome e sem valor (ADR-008).
/// </remarks>
public sealed class DuplicateProbe : ValueObject
{
    public bool Exists { get; private set; }
    public bool BelongsToTenant { get; private set; }
    public BillId? OriginalBillId { get; private set; }

    private DuplicateProbe() { }

    public static DuplicateProbe NotFound() => new();

    public static DuplicateProbe FoundInTenant(BillId originalBillId)
        => new() { Exists = true, BelongsToTenant = true, OriginalBillId = originalBillId };

    public static DuplicateProbe FoundInAnotherTenant() => new() { Exists = true };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Exists;
        yield return BelongsToTenant;
        yield return OriginalBillId;
    }
}
