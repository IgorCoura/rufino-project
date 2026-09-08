namespace BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;

/// <summary>
/// As catorze verificações do catálogo (<c>03-bill-validation.md</c>). Uma por documento, sempre —
/// a que não se aplica é registrada como <c>Skipped</c>, nunca omitida.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DefaultSeverity"/> é o peso <em>usual</em> do check. Alguns escapam dele em
/// situações específicas — banco cujas duas fontes autoritativas discordam, pagador extraído
/// que contradiz o cadastro, origem explicitamente banida — e por isso a severidade viaja no
/// <see cref="CheckResult"/>, não só aqui.
/// </para>
/// <para>
/// <strong>Acrescentar um valor invalida aprovações pendentes até a revalidação</strong>
/// (invariante 3 do <c>Bill</c>), e é o comportamento desejado: um check novo é uma pergunta
/// que ninguém ainda respondeu para aquele boleto.
/// </para>
/// </remarks>
public sealed class CheckType : Enumeration
{
    /// <summary>A linha digitável ou o BR Code são estruturalmente válidos?</summary>
    public static readonly CheckType BarcodeIntegrity = new(1, nameof(BarcodeIntegrity), CheckSeverity.Blocking);

    /// <summary>Já pagamos — ou já vamos pagar — este mesmo compromisso? Busca <strong>global</strong>.</summary>
    public static readonly CheckType Duplicate = new(2, nameof(Duplicate), CheckSeverity.Blocking);

    /// <summary>A consulta oficial respondeu? Nunca cai para "aprova sem consulta".</summary>
    public static readonly CheckType LookupAvailability = new(3, nameof(LookupAvailability), CheckSeverity.Blocking);

    /// <summary>O que dá para ler offline bate com o que o sistema bancário devolveu?</summary>
    public static readonly CheckType LookupConsistency = new(4, nameof(LookupConsistency), CheckSeverity.Blocking);

    /// <summary>O beneficiário condiz com o cadastro?</summary>
    public static readonly CheckType PayeeMatch = new(5, nameof(PayeeMatch), CheckSeverity.Blocking);

    /// <summary>O banco recebedor condiz? Fonte é o próprio código de barras, não a consulta.</summary>
    public static readonly CheckType ReceivingBankMatch = new(6, nameof(ReceivingBankMatch), CheckSeverity.Advisory);

    /// <summary>O valor condiz com a política do beneficiário?</summary>
    public static readonly CheckType AmountMatch = new(7, nameof(AmountMatch), CheckSeverity.Advisory);

    /// <summary>O pagador impresso condiz com o tenant? Contradição bloqueia; ausência não libera.</summary>
    public static readonly CheckType PayerMatch = new(8, nameof(PayerMatch), CheckSeverity.Advisory);

    /// <summary>Veio de origem confiável? Origem confiável nunca compensa beneficiário errado.</summary>
    public static readonly CheckType OriginTrust = new(9, nameof(OriginTrust), CheckSeverity.Advisory);

    /// <summary>
    /// Dá tempo de pagar? <strong>Teto de Atenção</strong> (<see cref="CheckSeverity.Notice"/>):
    /// boleto vencido, fora do corte do provedor ou sem data agendável é problema de calendário,
    /// não sinal de fraude — e mandá-lo para Perigo faria a conta atrasada exigir o "assumo o
    /// risco" (decisão do usuário, 2026-09-08).
    /// </summary>
    public static readonly CheckType DueDateSanity = new(10, nameof(DueDateSanity), CheckSeverity.Notice);

    /// <summary>Por qual degrau da escada este boleto foi atribuído a este tenant?</summary>
    public static readonly CheckType TenantRouting = new(11, nameof(TenantRouting), CheckSeverity.Advisory);

    /// <summary>O QR Pix e o código de barras contam a mesma história?</summary>
    public static readonly CheckType PixBarcodeConsistency = new(12, nameof(PixBarcodeConsistency), CheckSeverity.Blocking);

    /// <summary>
    /// O que está impresso no documento (lido pela IA) bate com a consulta oficial? Advisory por
    /// padrão — valor e vencimento divergentes viram aviso —, mas identidade do beneficiário
    /// divergente escala para Blocking: é o vetor da fraude de instrumento trocado sobre
    /// documento legítimo (Fase E, 2026-08-27).
    /// </summary>
    public static readonly CheckType DocumentConsistency = new(13, nameof(DocumentConsistency), CheckSeverity.Advisory);

    /// <summary>
    /// Esta conta era esperada? Cruza o boleto com os ciclos de <c>BillExpectation</c> do
    /// beneficiário (ADR-020, 2026-09-08).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Teto de Atenção</strong> (<see cref="CheckSeverity.Notice"/>) por natureza: não
    /// haver expectativa não desmente nada — a maior parte dos boletos legítimos nunca teve uma.
    /// O que o check acrescenta é o inverso do alerta do ADR-014: lá o sistema avisa que a conta
    /// esperada não chegou; aqui ele avisa que chegou uma conta que ninguém esperava.
    /// </para>
    /// <para>
    /// Quem apura é o <c>ExpectationMatchingService</c> — o <strong>mesmo</strong> serviço que o
    /// cumprimento usa, nunca uma cópia da regra: se os dois divergissem, a tela diria "conta
    /// esperada" sobre um ciclo que segue alertando sozinho.
    /// </para>
    /// </remarks>
    public static readonly CheckType ExpectationMatch = new(14, nameof(ExpectationMatch), CheckSeverity.Notice);

    /// <summary>Peso usual da falha deste check. O resultado pode carregar outro.</summary>
    public CheckSeverity DefaultSeverity { get; }

    private CheckType(int id, string name, CheckSeverity defaultSeverity) : base(id, name)
        => DefaultSeverity = defaultSeverity;
}
