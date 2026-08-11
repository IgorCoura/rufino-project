namespace BillPayment.Domain.CaptureItems;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Qual degrau da cascata de extração resolveu este artefato.
/// </summary>
/// <remarks>
/// <para>
/// Não é rótulo decorativo: é a métrica que diz se a cascata está fazendo o que se espera dela.
/// A cascata é <strong>ordenada por custo</strong> (doc 09), e um degrau barato perdendo terreno
/// para um caro é sinal de regressão no parser — mudança que, sem esta medida, só apareceria na
/// fatura do provedor de IA.
/// </para>
/// <para>
/// O que decide continua sendo o DV e a consulta oficial, qualquer que seja o degrau
/// (ADR-011). <see cref="Vision"/> aqui significa "quem <em>propôs</em> o candidato foi o
/// modelo", nunca "o modelo confirmou".
/// </para>
/// </remarks>
public sealed class ExtractionMethod : Enumeration
{
    /// <summary>Texto embutido no PDF — degrau 2, gratuito e instantâneo.</summary>
    public static readonly ExtractionMethod EmbeddedText = new(1, nameof(EmbeddedText));

    /// <summary>QR Code rasterizado e decodificado localmente — degrau 2b (sprint 2.3).</summary>
    public static readonly ExtractionMethod QrCode = new(2, nameof(QrCode));

    /// <summary>Extrator de visão — degrau 3, o caro (sprint 2.4).</summary>
    public static readonly ExtractionMethod Vision = new(3, nameof(Vision));

    /// <summary>Uma pessoa informou a linha digitável — degrau 4, a quarentena resolvida à mão.</summary>
    public static readonly ExtractionMethod Manual = new(4, nameof(Manual));

    private ExtractionMethod(int id, string name) : base(id, name) { }
}
