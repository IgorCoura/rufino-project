namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Como a escada de resolução de link terminou.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque "não resolveu" não é uma informação.</strong> Até aqui, link expirado,
/// emissor sem receita, host recusado por faixa de IP e escada que desceu cinco níveis sem achar
/// nada produziam exatamente o mesmo desfecho — item na quarentena, sem motivo. Quem olhava a
/// fila não tinha como separar o que precisa de cadastro do que precisa de conserto, e foi por
/// isso que a falha da Acessórias ficou invisível por semanas.
/// </para>
/// <para>
/// <strong>É desfecho, não exceção.</strong> A escada degrada: o que ela não alcança segue para
/// a quarentena, onde uma pessoa decide. Lançar aqui contaria tentativa contra o item e mataria
/// o worker por causa de um servidor de terceiro fora do ar — a mesma lição que fixou o
/// <c>null</c> em vez do <c>throw</c> quando a escada nasceu.
/// </para>
/// </remarks>
public sealed class LinkResolutionOutcome : Enumeration
{
    /// <summary>O documento foi trazido.</summary>
    public static readonly LinkResolutionOutcome Resolved = new(1, nameof(Resolved), deservesAttention: false);

    /// <summary>Não havia endereço nenhum no corpo — mensagem sem link.</summary>
    public static readonly LinkResolutionOutcome NoCandidates = new(2, nameof(NoCandidates), deservesAttention: false);

    /// <summary>
    /// Havia endereço, mas nenhum casou receita e o regime é fechado.
    /// </summary>
    /// <remarks>
    /// <strong>É a fila de trabalho.</strong> Este desfecho, somado ao <c>LinkHost</c>, responde
    /// "quais emissores mandam boleto por link que ainda não sabemos buscar" — que é exatamente
    /// a pergunta que ninguém conseguia responder antes.
    /// </remarks>
    public static readonly LinkResolutionOutcome NoRecipe = new(3, nameof(NoRecipe), deservesAttention: true);

    /// <summary>Desceu todos os níveis permitidos e não achou documento.</summary>
    public static readonly LinkResolutionOutcome DepthExhausted = new(4, nameof(DepthExhausted), deservesAttention: true);

    /// <summary>Gastou as requisições permitidas para a mensagem antes de achar.</summary>
    public static readonly LinkResolutionOutcome BudgetExhausted = new(5, nameof(BudgetExhausted), deservesAttention: true);

    /// <summary>
    /// Todos os candidatos foram recusados pelas travas de rede — faixa de IP, porta ou host.
    /// </summary>
    /// <remarks>
    /// <strong>Este desfecho merece olhar.</strong> Emissor honesto não hospeda boleto em
    /// <c>127.0.0.1</c>: repetido, é sinal de que alguém está usando a caixa de entrada para
    /// sondar a rede interna a partir de dentro.
    /// </remarks>
    public static readonly LinkResolutionOutcome Refused = new(6, nameof(Refused), deservesAttention: true);

    /// <summary>Os endereços responderam erro, redirecionamento ou nada.</summary>
    public static readonly LinkResolutionOutcome Unreachable = new(7, nameof(Unreachable), deservesAttention: false);

    /// <summary>O remetente estourou o teto diário de buscas.</summary>
    public static readonly LinkResolutionOutcome Throttled = new(8, nameof(Throttled), deservesAttention: true);

    /// <summary>A escada está desligada nesta instalação.</summary>
    public static readonly LinkResolutionOutcome Disabled = new(9, nameof(Disabled), deservesAttention: false);

    private LinkResolutionOutcome(int id, string name, bool deservesAttention)
        : base(id, name)
        => DeservesAttention = deservesAttention;

    /// <summary>
    /// Se o desfecho descreve algo que uma pessoa deveria consertar, e não o curso normal.
    /// </summary>
    /// <remarks>
    /// <see cref="NoCandidates"/> e <see cref="Unreachable"/> ficam de fora: mensagem sem link é o
    /// caso comum de uma caixa de uso misto, e link expirado é o desfecho esperado de um boleto
    /// que ficou parado. Marcar os dois como pendência encheria a fila de conversa e propaganda —
    /// e uma fila que ninguém olha é pior que fila nenhuma.
    /// </remarks>
    public bool DeservesAttention { get; }
}
