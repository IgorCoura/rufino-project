namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O pagador como o <strong>documento</strong> o declara — lido do PDF, não de fonte oficial.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É um tipo diferente de <c>LookupParty</c> de propósito</strong>, apesar de guardar
/// quase os mesmos campos. A diferença não é de forma, é de autoridade: <c>LookupParty</c> vem
/// da consulta oficial e sustenta reprovação; este vem de um PDF que ninguém certifica e só
/// pode <em>contradizer</em> (ADR-004). Tipos distintos impedem que um seja passado onde o
/// outro é esperado — que é exatamente o erro que apagaria a distinção.
/// </para>
/// <para>
/// Nulo é o caso majoritário <strong>por medição</strong>: o CNPJ do pagador aparece em apenas
/// 38% dos boletos reais do corpus. Contas de concessionária identificam o pagador por conta
/// contrato ou matrícula, não por documento fiscal.
/// </para>
/// </remarks>
public sealed class PartyInfo : ValueObject
{
    public const int NAME_MAX_LENGTH = 200;

    public string? Name { get; private set; }
    public TaxId? TaxId { get; private set; }

    private PartyInfo() { }

    public static PartyInfo Of(string? name, TaxId? taxId)
    {
        var trimmed = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        if (trimmed is { Length: > NAME_MAX_LENGTH })
            trimmed = trimmed[..NAME_MAX_LENGTH];

        if (trimmed is null && taxId is null)
            throw BillErrors.ExtractedPartyWithoutAnyIdentifier();

        return new PartyInfo { Name = trimmed, TaxId = taxId };
    }

    /// <summary>
    /// Compõe a partir do texto cru extraído do documento. Documento sem dígito verificador
    /// válido vira ausência — a extração produz <em>candidatos</em>, e o DV é quem decide
    /// (ADR-011).
    /// </summary>
    public static PartyInfo? FromExtraction(string? name, string? taxId)
    {
        var parsed = SharedKernel.TaxId.TryParse(taxId, out var value) ? value : null;

        return string.IsNullOrWhiteSpace(name) && parsed is null ? null : Of(name, parsed);
    }

    /// <summary>Só há o que comparar contra o cadastro quando o documento fiscal foi extraído.</summary>
    public bool HasTaxId => TaxId is not null;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return TaxId;
    }
}
