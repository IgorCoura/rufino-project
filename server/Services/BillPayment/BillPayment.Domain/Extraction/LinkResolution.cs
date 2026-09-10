namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que a escada de resolução de link conseguiu, e o que ela tentou.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Substituiu o <c>ResolvedDocument?</c> devolvido antes, e a diferença é o
/// <c>null</c>.</strong> Nulo dizia "não resolveu" e mais nada — link expirado, emissor sem
/// receita, host recusado por faixa de IP e escada que desceu cinco níveis sem achar produziam
/// exatamente o mesmo silêncio. Quem olhava a quarentena não tinha como separar o que precisa de
/// cadastro do que precisa de conserto.
/// </para>
/// <para>
/// <strong>O candidato viaja junto porque é a fila de trabalho.</strong> Saber <em>para onde</em>
/// a escada teria ido é o que transforma "não consegui" em "cadastre este emissor" — e era
/// justamente a informação que se perdia no caso em que ela é necessária.
/// </para>
/// </remarks>
public sealed class LinkResolution : ValueObject
{
    /// <summary>Como terminou.</summary>
    public LinkResolutionOutcome Outcome { get; }

    /// <summary>O documento, quando houve.</summary>
    public ResolvedDocument? Document { get; }

    /// <summary>
    /// O endereço mais promissor que a escada viu — resolvido ou não.
    /// </summary>
    /// <remarks>
    /// <strong>É o melhor candidato, não o primeiro.</strong> A versão anterior guardava o
    /// primeiro <c>&lt;a&gt;</c> do e-mail, que em mensagem de campanha é o logotipo ou o "ver no
    /// navegador" — então a quarentena registrava o host de um rastreador, e o mecanismo que
    /// existia para descobrir emissor novo apontava para o lugar errado. Foi por isso que a falta
    /// da receita da Acessórias passou semanas invisível.
    /// </remarks>
    public DocumentLink? BestCandidate { get; }

    /// <summary>Quantas requisições a mensagem provocou.</summary>
    public int Fetches { get; }

    /// <summary>O nível mais fundo alcançado — 0 quando nada foi buscado.</summary>
    public int DeepestLevel { get; }

    private LinkResolution(
        LinkResolutionOutcome outcome,
        ResolvedDocument? document,
        DocumentLink? bestCandidate,
        int fetches,
        int deepestLevel)
    {
        Outcome = outcome;
        Document = document;
        BestCandidate = bestCandidate;
        Fetches = fetches;
        DeepestLevel = deepestLevel;
    }

    /// <summary>A escada trouxe o documento.</summary>
    public static LinkResolution Resolved(ResolvedDocument document, DocumentLink? origin, int fetches, int deepestLevel)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new LinkResolution(LinkResolutionOutcome.Resolved, document, origin, fetches, deepestLevel);
    }

    /// <summary>A escada não trouxe o documento, e este é o motivo.</summary>
    /// <remarks>
    /// Não lança: link expirado, host fora do ar e emissor sem receita são o curso normal de uma
    /// caixa de uso misto, e a mensagem segue para a quarentena como qualquer outro artefato que a
    /// cascata não resolveu.
    /// </remarks>
    public static LinkResolution Failed(
        LinkResolutionOutcome outcome,
        DocumentLink? bestCandidate = null,
        int fetches = 0,
        int deepestLevel = 0)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return new LinkResolution(outcome, document: null, bestCandidate, fetches, deepestLevel);
    }

    /// <summary>A escada está desligada nesta instalação — nada foi tentado.</summary>
    public static LinkResolution Disabled() => Failed(LinkResolutionOutcome.Disabled);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Outcome;
        yield return Document;
        yield return BestCandidate;
        yield return Fetches;
        yield return DeepestLevel;
    }
}
