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
/// <strong>Nulo deixou de ser o caso majoritário</strong>, e o número que dizia o contrário foi
/// reconciliado na sprint 2.6: os 38% do doc 08 contavam todos os arquivos do corpus, inclusive
/// os que não têm camada de texto. Sobre os documentos que produzem instrumento — que são os
/// únicos que chegam aqui — o documento fiscal do pagador aparece em <strong>93,3%</strong>
/// (326 documentos, 14 meses). Continua nulo quando a concessionária identifica o pagador só por
/// conta contrato ou matrícula, e quando a escada não constatou de quem é o boleto.
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
