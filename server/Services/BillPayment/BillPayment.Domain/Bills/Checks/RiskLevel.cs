namespace BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;

/// <summary>
/// A classificação de risco do boleto, derivada das verificações — Seguro, Atenção ou Perigo.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Substitui a rejeição automática (ADR-015, 2026-08-27).</strong> Até então, falha
/// bloqueante levava o boleto direto a <c>Rejected</c>; a decisão do usuário mudou o modelo: o
/// sistema classifica e destaca, e quem decide é sempre o humano. Falha que era bloqueante vira
/// <see cref="Danger"/> — aprovável somente com o risco explicitamente assumido, gravado na
/// trilha de auditoria.
/// </para>
/// <para>
/// O que continua fora do alcance da classificação: linha digitável com DV inválido nunca vira
/// <c>Bill</c> (integridade estrutural, não veto), e a deduplicação na captura continua não
/// criando segundo boleto para o mesmo instrumento.
/// </para>
/// </remarks>
public sealed class RiskLevel : Enumeration
{
    /// <summary>Todas as verificações passaram (ou não se aplicam) — caminho limpo.</summary>
    public static readonly RiskLevel Safe = new(1, nameof(Safe));

    /// <summary>Algo inconclusivo ou divergência leve — o aprovador confere antes de autorizar.</summary>
    public static readonly RiskLevel Attention = new(2, nameof(Attention));

    /// <summary>
    /// Sinal com cara de fraude ou de pagamento duplicado — aprovar exige assumir o risco
    /// explicitamente.
    /// </summary>
    public static readonly RiskLevel Danger = new(3, nameof(Danger));

    private RiskLevel(int id, string name) : base(id, name) { }
}
