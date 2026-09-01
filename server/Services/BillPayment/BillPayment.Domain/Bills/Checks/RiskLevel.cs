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
    public static readonly RiskLevel Safe = new(1, nameof(Safe), tier: 1);

    /// <summary>Algo inconclusivo ou divergência leve — o aprovador confere antes de autorizar.</summary>
    public static readonly RiskLevel Attention = new(2, nameof(Attention), tier: 2);

    /// <summary>
    /// Sinal com cara de fraude ou de pagamento duplicado — aprovar exige assumir o risco
    /// explicitamente.
    /// </summary>
    public static readonly RiskLevel Danger = new(3, nameof(Danger), tier: 3);

    /// <summary>
    /// O próprio tenant declarou o ator hostil: beneficiário na blacklist ou origem bloqueada.
    /// Suspeita derivada é <see cref="Danger"/>; declaração explícita é isto — e aprovar exige
    /// a alçada máxima além do aceite.
    /// </summary>
    public static readonly RiskLevel ExtremeDanger = new(4, nameof(ExtremeDanger), tier: 4);

    // A ordem da escala é dado próprio, não o Id do Smart Enum: id é identidade de
    // persistência, e ninguém deveria poder "ordenar" Enumeration por acidente.
    private readonly int _tier;

    private RiskLevel(int id, string name, int tier) : base(id, name) => _tier = tier;

    /// <summary>A alçada cobre este risco? Hierárquica: quem aprova Perigo aprova Atenção.</summary>
    public bool IsCoveredBy(RiskLevel clearance) => clearance is not null && _tier <= clearance._tier;
}
