namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Um documento fiscal lido do artefato, com a indicação de ter sido encontrado sob rótulo de
/// pagador.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Um boleto carrega o documento das duas partes.</strong> O CNPJ do beneficiário está
/// impresso ao lado do CNPJ do pagador, e nada no número distingue um do outro — por isso a
/// leitura devolve <em>candidatos</em>, e quem decide o papel de cada um é o
/// <c>BillRoutingService</c> confrontando com o cadastro.
/// </para>
/// <para>
/// <strong><see cref="UnderPayerLabel"/> não é enfeite: é o que autoriza a afirmação negativa.</strong>
/// Medido sobre 326 boletos reais em 2026-08-12: o documento do tenant aparece em 93,3% deles e
/// em <strong>0%</strong> aparece do lado do beneficiário — então achar um documento cadastrado
/// basta para atribuir o boleto. O contrário não vale: concluir "este boleto é de outra pessoa"
/// a partir de um número sem rótulo confundiria o CNPJ da concessionária com o do pagador e
/// mandaria para a quarentena cega uma conta que o usuário poderia reivindicar. Só 66,8% das
/// ocorrências têm rótulo por perto, e é sobre essas que a negativa pode se apoiar.
/// </para>
/// </remarks>
public sealed class PartyCandidate : ValueObject
{
    public TaxId TaxId { get; }

    /// <summary>
    /// O número apareceu logo depois de "pagador", "sacado", "tomador", "cliente" ou
    /// "contribuinte" — os cinco rótulos observados no corpus.
    /// </summary>
    public bool UnderPayerLabel { get; }

    private PartyCandidate(TaxId taxId, bool underPayerLabel)
    {
        TaxId = taxId;
        UnderPayerLabel = underPayerLabel;
    }

    /// <summary>
    /// Compõe a partir do texto cru. Documento sem dígito verificador válido vira ausência — a
    /// leitura produz candidatos e o DV é quem decide (ADR-011).
    /// </summary>
    public static PartyCandidate? TryCreate(string? taxId, bool underPayerLabel = false)
        => TaxId.TryParse(taxId, out var parsed) && parsed is not null
            ? new PartyCandidate(parsed, underPayerLabel)
            : null;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TaxId;
        yield return UnderPayerLabel;
    }
}
