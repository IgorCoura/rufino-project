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

    /// <summary>
    /// O pior dos dois. É como <c>RecordChecks</c> agrega as contribuições das verificações — a
    /// flag mede a pior evidência encontrada, e "pior" é a escala explícita, nunca o Id.
    /// </summary>
    public RiskLevel Worst(RiskLevel other)
        => other is null || other._tier <= _tier ? this : other;

    /// <summary>
    /// Quanto uma verificação isolada pesa na classificação. <strong>É a régua inteira, num
    /// lugar só</strong> — <c>CheckResult</c> e <c>BillCheck</c> apenas delegam para cá.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Substituiu em 2026-09-08 a cadeia de <c>if</c> que vivia dentro de <c>RecordChecks</c>
    /// (ADR-020). A troca não é estética: com a régua declarada por desfecho, o teto de
    /// <see cref="CheckSeverity.Notice"/> fica visível no tipo em vez de virar caso especial
    /// escondido num <c>else if</c>.
    /// </para>
    /// <para>
    /// <strong>Advisory deixou de ser Atenção.</strong> Até esta data, falha advisory, aviso e
    /// inconclusivo paravam em <see cref="Attention"/>; agora sobem para <see cref="Danger"/>, e
    /// quem tem teto de Atenção é só quem foi marcado <c>Notice</c> — expectativa, prazo e nome
    /// do beneficiário.
    /// </para>
    /// </remarks>
    internal static RiskLevel Of(CheckOutcome outcome, CheckSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(severity);

        // Passou ou não se aplica: não há evidência nenhuma a pesar.
        if (!outcome.RequiresAttention)
            return Safe;

        // Declaração explícita do tenant. Só a FALHA é declaração — um aviso num check crítico
        // continua sendo aviso, e cair para Danger aqui é a resposta segura.
        if (severity == CheckSeverity.Critical)
            return outcome.IsFailure ? ExtremeDanger : Danger;

        return severity == CheckSeverity.Notice ? Attention : Danger;
    }
}
