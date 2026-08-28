namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;

/// <summary>
/// Extrator de documentos falso e programável.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A suíte nunca chama a rede</strong> (doc 10, guardrail 7). Além do custo e da cota, um
/// teste que dependesse do modelo real seria não determinístico: a mesma entrada poderia devolver
/// respostas diferentes, e um teste que às vezes passa não prova nada.
/// </para>
/// <para>
/// <strong>Existe sobretudo para devolver resposta ERRADA.</strong> O teste mais valioso do
/// conjunto não é o que prova que a visão resolve um boleto — é o que prova que uma linha
/// digitável alucinada é barrada pelo dígito verificador e não vira <c>Bill</c> (ADR-011).
/// </para>
/// </remarks>
internal sealed class FakeDocumentIntelligence : IDocumentIntelligence
{
    /// <summary>Ligado por padrão: quem pede este host quer exercitar o degrau de visão.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>O que o "modelo" devolve. Vazio por padrão — o desfecho mais comum.</summary>
    public ExtractedDocument Result { get; set; } = ExtractedDocument.Empty;

    /// <summary>
    /// Desfecho a simular. <strong>Existe para o teste poder dizer "o provedor caiu"</strong> —
    /// até 2026-08-27 o fake só sabia devolver resultado, e por isso não havia teste nenhum
    /// cobrindo indisponibilidade: a porta não sabia expressá-la.
    /// </summary>
    public ExtractionStatus Outcome { get; set; } = ExtractionStatus.Resolved;

    /// <summary>Quantas vezes foi chamado — é o que prova que o portão de gasto funcionou.</summary>
    public int CallCount { get; private set; }

    /// <summary>Os tipos de mídia recebidos, para provar que imagem também chega aqui.</summary>
    public List<string> ReceivedMediaTypes { get; } = [];

    /// <summary>As dicas recebidas, para provar que só dado do próprio tenant sai do perímetro.</summary>
    public ExtractionHints? LastHints { get; private set; }

    public Task<ExtractionAttempt> ExtractAsync(
        DocumentPayload payload,
        ExtractionHints hints,
        CancellationToken cancellationToken)
    {
        CallCount++;
        ReceivedMediaTypes.Add(payload.MediaType);
        LastHints = hints;

        if (Outcome == ExtractionStatus.Unavailable)
            return Task.FromResult(ExtractionAttempt.Unavailable("provider_503"));

        if (Outcome == ExtractionStatus.Rejected)
            return Task.FromResult(ExtractionAttempt.Rejected("provider_400"));

        if (Outcome == ExtractionStatus.BudgetExhausted)
            return Task.FromResult(ExtractionAttempt.BudgetExhausted());

        return Task.FromResult(ExtractionAttempt.Answered(Result));
    }
}
