namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// De onde saiu o vencimento consolidado do boleto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe para a evidência dizer a procedência</strong>, e por isso é derivado — não há
/// coluna, e o mapping o ignora. Um vencimento constatado na consulta oficial e um transcrito
/// por modelo pesam diferente para quem aprova, e a tela precisa poder distinguir os dois sem
/// que o check recalcule a precedência por conta própria (era assim, e produzia duas datas
/// diferentes na mesma apuração).
/// </para>
/// </remarks>
public sealed class DueDateSource : Enumeration
{
    /// <summary>Consulta oficial do trilho que paga, ou do outro como reserva.</summary>
    public static readonly DueDateSource Official = new(1, nameof(Official));

    /// <summary>Fator de vencimento embutido no código de barras — protegido pelo DV geral.</summary>
    public static readonly DueDateSource Barcode = new(2, nameof(Barcode));

    /// <summary>Leitura por IA. A última reserva, e a única que ninguém certifica.</summary>
    public static readonly DueDateSource Reading = new(3, nameof(Reading));

    /// <summary>Nenhuma fonte trouxe vencimento.</summary>
    public static readonly DueDateSource None = new(4, nameof(None));

    private DueDateSource(int id, string name) : base(id, name) { }
}
