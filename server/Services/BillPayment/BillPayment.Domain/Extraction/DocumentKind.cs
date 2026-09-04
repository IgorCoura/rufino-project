namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Que tipo de documento o extrator de visão acha que leu.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É opinião, não classificação confiável</strong> — serve para métrica e para a
/// evidência do check, nunca para decidir pagamento. Quem diz o que o documento é de verdade é o
/// instrumento que sobreviveu ao DV e à consulta oficial (ADR-011).
/// </para>
/// <para>
/// <see cref="NotABill"/> existe para o modelo poder dizer "isto é uma nota fiscal" em vez de
/// inventar uma linha digitável — e alucinação sob pressão é exatamente o que se quer evitar
/// quando 4 de cada 10 documentos da fila não são boleto.
/// </para>
/// </remarks>
public sealed class DocumentKind : Enumeration
{
    /// <summary>Boleto de cobrança registrado — o caso com beneficiário e banco.</summary>
    public static readonly DocumentKind BankSlip = new(1, nameof(BankSlip));

    /// <summary>Conta de concessionária: luz, água, gás, telefonia.</summary>
    public static readonly DocumentKind Utility = new(2, nameof(Utility));

    /// <summary>Guia de arrecadação: FGTS, DARF, GPS, sindicato.</summary>
    public static readonly DocumentKind TaxGuide = new(3, nameof(TaxGuide));

    /// <summary>Nota fiscal, holerite, contrato — o que a fila tem de sobra.</summary>
    public static readonly DocumentKind NotABill = new(4, nameof(NotABill));

    private DocumentKind(int id, string name) : base(id, name) { }
}
