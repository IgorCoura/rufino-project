namespace BillPayment.Domain.Services;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.TrustedOrigins;

/// <summary>
/// O destino de um artefato depois que a cascata de extração rodou.
/// </summary>
public sealed class CaptureTriageDecision : Enumeration
{
    /// <summary>Achou instrumento válido. Segue para a consulta oficial e o roteamento.</summary>
    public static readonly CaptureTriageDecision Parse = new(1, nameof(Parse));

    /// <summary>PDF cifrado de remetente conhecido. Aguarda a senha de uma pessoa.</summary>
    public static readonly CaptureTriageDecision Lock = new(2, nameof(Lock));

    /// <summary>
    /// Nada encontrado, <strong>mas o remetente é cadastrado</strong> — provável falha do
    /// parser, não ausência de boleto. Fica na fila para alguém informar a linha à mão.
    /// </summary>
    public static readonly CaptureTriageDecision Quarantine = new(3, nameof(Quarantine));

    /// <summary>
    /// Nada encontrado e remetente desconhecido. <strong>O item não chega a existir</strong>:
    /// nem registro, nem arquivo guardado.
    /// </summary>
    public static readonly CaptureTriageDecision Drop = new(4, nameof(Drop));

    private CaptureTriageDecision(int id, string name) : base(id, name) { }
}

/// <summary>
/// Decide o que fazer com um artefato que passou pela cascata de extração.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É Domain Service porque cruza dois Aggregates</strong> — o artefato capturado e a
/// origem confiável do tenant. Estático e puro, como <c>BillValidationService</c>: sem estado,
/// sem I/O, sem relógio. Quem busca a origem é o handler; aqui só entra o resultado.
/// </para>
/// <para>
/// <strong>A regra central: descartar é o padrão.</strong> Decisão do usuário em 2026-08-11 — um
/// balde cheio de e-mail irrelevante é um balde que ninguém olha, e uma fila que ninguém olha é
/// pior do que fila nenhuma, porque dá a impressão de que alguém está olhando. O descarte é
/// seguro porque o filtro é <em>determinístico</em>: só sobrevive documento com DV ou CRC
/// conferido, e contrato, CNH ou apresentação não têm nenhum dos dois.
/// </para>
/// <para>
/// <strong>E a exceção: remetente cadastrado.</strong> Quando quem mandou é uma origem que o
/// próprio tenant cadastrou, "não achei boleto" é mais provavelmente falha do parser do que
/// ausência de boleto — e descartar perderia a conta de um fornecedor conhecido. O volume dessa
/// exceção é pequeno por construção.
/// </para>
/// </remarks>
public static class CaptureTriageService
{
    /// <param name="origin">
    /// A origem que casou com o remetente, ou <c>null</c> quando desconhecida — que é estado
    /// válido e comum, não erro.
    /// </param>
    /// <param name="subject">
    /// Assunto da mensagem — evidência fraca, usada só para NÃO descartar. O endereço do
    /// remetente não entra: a comparação é por substring e "conta" casa em "contabilidade@",
    /// que é o endereço do contador e a maior fonte de ruído medida na caixa real.
    /// </param>
    public static CaptureTriageDecision Decide(
        ExtractionResult extraction,
        TrustedOrigin? origin,
        string? subject = null)
    {
        ArgumentNullException.ThrowIfNull(extraction);

        if (extraction.Resolved)
            return CaptureTriageDecision.Parse;

        // Origem banida não ganha a exceção: o tenant já disse que não quer nada dali, e manter
        // o item contrariaria a decisão dele em vez de protegê-lo. `IsStrong` já trata isso —
        // origem banida não conta como forte, e as palavras sozinhas também não a ressuscitam.
        var banned = origin is not null && origin.Decision == TrustDecision.Blocked;

        // A SEGUNDA exceção ao descarte, acrescentada em 2026-08-26: evidência forte de cobrança.
        // A primeira (remetente cadastrado) só alcança quem o tenant já conhece, e por isso o
        // sistema nunca descobria emissor novo — a conta chegava, não tinha anexo, e sumia sem
        // deixar rastro. Medido na caixa real: das 23 mensagens sem anexo, só 3 têm sinal de
        // cobrança, então a fila continua utilizável. O sinal decide GUARDAR, nunca descartar.
        if (banned || !BillingSignal.IsStrong(origin, subject))
            return CaptureTriageDecision.Drop;

        // Cifrado é diferente de "não é boleto": aqui não se sabe o que há dentro, e o próprio
        // fato de não saber é o que exige a intervenção humana.
        return extraction.IsLocked ? CaptureTriageDecision.Lock : CaptureTriageDecision.Quarantine;
    }
}
