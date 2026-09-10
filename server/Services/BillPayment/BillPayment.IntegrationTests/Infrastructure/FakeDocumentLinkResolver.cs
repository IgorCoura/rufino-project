namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;

/// <summary>
/// Resolvedor de link falso e programável.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A suíte nunca busca endereço de verdade.</strong> A escada de link é o único ponto do
/// BC que faz requisição para servidor de <em>terceiro</em> — não para um provedor com quem há
/// contrato. Um teste que dependesse disso mediria se o emissor está no ar, e quebraria numa
/// máquina sem saída para a internet.
/// </para>
/// <para>
/// <strong>Existe sobretudo para devolver o documento errado.</strong> O que precisa ser provado
/// não é que o download funciona: é que um PDF trazido de um link atravessa <em>a mesma</em>
/// cascata determinística de um anexo, e que nada nele dispensa DV ou CRC.
/// </para>
/// </remarks>
internal sealed class FakeDocumentLinkResolver : IDocumentLinkResolver
{
    /// <summary>Ligado por padrão: quem pede este host quer exercitar o degrau de link.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Hosts que o portão de ingestão deve considerar buscáveis. Vazio por padrão — assim os
    /// testes que já existiam continuam capturando o corpo só por sinal escrito no texto.
    /// </summary>
    public List<string> Hosts { get; } = [];

    /// <summary>O documento devolvido. Nulo por padrão — o desfecho mais comum.</summary>
    public ResolvedDocument? Result { get; set; }

    /// <summary>
    /// O desfecho devolvido quando não há documento.
    /// </summary>
    /// <remarks>
    /// <c>NoRecipe</c> por padrão porque é o caso que a suíte mais exercita: emissor que a escada
    /// não sabe buscar. Quem quer provar o teto ou a recusa de rede troca por outro.
    /// </remarks>
    public LinkResolutionOutcome FailureOutcome { get; set; } = LinkResolutionOutcome.NoRecipe;

    /// <summary>Quantas vezes foi chamado — é o que prova que o degrau anterior resolveu sozinho.</summary>
    public int CallCount { get; private set; }

    /// <summary>O corpo recebido, para conferir que o degrau 2 opera sobre o e-mail e não o anexo.</summary>
    public string? LastBody { get; private set; }

    public IReadOnlyCollection<string> ResolvableHosts => Hosts;

    /// <summary>
    /// O endereço que a escada reporta como procedência — o link do e-mail, não o último salto.
    /// </summary>
    public DocumentLink? Candidate { get; set; }

    /// <summary>O remetente recebido, para conferir que o teto diário é alimentado.</summary>
    public string? LastSender { get; private set; }

    public Task<LinkResolution> ResolveAsync(
        ReadOnlyMemory<byte> body,
        string? contentType,
        string? sender,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastBody = System.Text.Encoding.UTF8.GetString(body.Span);
        LastSender = sender;

        if (!IsEnabled)
            return Task.FromResult(LinkResolution.Disabled());

        if (Result is null)
            return Task.FromResult(LinkResolution.Failed(FailureOutcome, Candidate));

        // Em produção a escada SEMPRE conhece a raiz do ramo quando resolve — é ela que vira
        // procedência. Sem candidato explícito, o endereço do próprio documento faz esse papel,
        // senão o dublê descreveria uma resolução sem origem, que não existe.
        var origin = Candidate ?? DocumentLink.TryCreate(Result.SourceUrl);

        return Task.FromResult(LinkResolution.Resolved(Result, origin, fetches: 1, deepestLevel: 1));
    }
}
