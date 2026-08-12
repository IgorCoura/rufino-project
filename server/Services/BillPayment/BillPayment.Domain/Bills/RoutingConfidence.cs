namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Quanto a atribuição deste boleto a este tenant foi <em>inferida</em>, e não constatada.
/// </summary>
/// <remarks>
/// <para>
/// Corresponde aos degraus da escada de roteamento (<c>07-multitenancy-and-routing.md</c>). O
/// preenchimento vem da fase 2, quando a captura por e-mail existir; até lá só há importação
/// manual, que não passa pela escada e deixa o campo nulo.
/// </para>
/// <para>
/// O check que consome isto <strong>informa, não decide</strong>: aprovar um boleto que chegou
/// por <see cref="Weak"/> é decisão diferente de aprovar um que chegou por <see cref="Strong"/>,
/// e a tela precisa deixar isso visível.
/// </para>
/// </remarks>
public sealed class RoutingConfidence : Enumeration
{
    /// <summary>O documento fiscal do pagador extraído casou com o <c>PayerProfile</c>.</summary>
    public static readonly RoutingConfidence Strong = new(1, nameof(Strong), isConclusive: true);

    /// <summary>
    /// <strong>Sem produtor desde a sprint 2.6.</strong> Era o degrau 2 — uma <c>RoutingRule</c>
    /// aprendida casando por (beneficiário, referência de conta) —, abandonado quando a medição
    /// mostrou que a referência de conta não distingue pagadores (doc 07).
    /// </summary>
    /// <remarks>
    /// O valor permanece porque o id é persistido: removê-lo renumeraria os demais e reescreveria
    /// o significado de linhas já gravadas. Só volte a produzi-lo se o degrau 2 for reaberto com
    /// uma chave que de fato distinga tenants.
    /// </remarks>
    public static readonly RoutingConfidence Learned = new(2, nameof(Learned), isConclusive: true);

    /// <summary>O beneficiário é exclusivo deste tenant entre os que monitoram a fonte.</summary>
    public static readonly RoutingConfidence Weak = new(3, nameof(Weak), isConclusive: false);

    /// <summary>Um usuário reivindicou explicitamente o item na quarentena.</summary>
    public static readonly RoutingConfidence Claimed = new(4, nameof(Claimed), isConclusive: false);

    /// <summary>A atribuição foi constatada, e não apenas plausível.</summary>
    public bool IsConclusive { get; }

    private RoutingConfidence(int id, string name, bool isConclusive) : base(id, name)
        => IsConclusive = isConclusive;
}
