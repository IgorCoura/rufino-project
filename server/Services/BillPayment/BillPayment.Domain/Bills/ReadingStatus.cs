namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Em que pé está a leitura por IA de um boleto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O boleto NUNCA espera pela IA — quem espera é a análise.</strong> A captura não pode
/// ficar refém de um provedor externo: o boleto segue para aprovação com o que o funil
/// determinístico provou, e o retrato da leitura chega depois, se chegar. O que este estado
/// resolve é a honestidade da tela: até 2026-08-27 um boleto sem retrato era indistinguível de um
/// boleto cujo documento não tem o que ler, e metade deles estava sem retrato por causa de 503.
/// </para>
/// <para>
/// <strong>É o gêmeo do <c>CaptureItemStatus</c> na fila de captura</strong>, e pela mesma razão:
/// só existe fila com retentativa quando há onde registrar em que tentativa se está.
/// </para>
/// </remarks>
public sealed class ReadingStatus : Enumeration
{
    /// <summary>
    /// Não há o que ler — boleto importado só com os dígitos, mídia não suportada, extrator
    /// desligado. <strong>Ausência, não falha</strong>: a tela não mostra pendência nenhuma.
    /// </summary>
    public static readonly ReadingStatus NotApplicable = new(1, nameof(NotApplicable), isPending: false);

    /// <summary>Esperando a vez na fila. É o que a tela lê como "Na fila para análise".</summary>
    public static readonly ReadingStatus Queued = new(2, nameof(Queued), isPending: true);

    /// <summary>O retrato foi anexado.</summary>
    public static readonly ReadingStatus Done = new(3, nameof(Done), isPending: false);

    /// <summary>
    /// Desistiu depois de esgotar as tentativas, ou o provedor recusou o artefato. Não é
    /// terminal: quem opera pode reenfileirar, e é isso que "tentar de novo" significa.
    /// </summary>
    public static readonly ReadingStatus Unavailable = new(4, nameof(Unavailable), isPending: false);

    /// <summary>A fila ainda deve este boleto.</summary>
    public bool IsPending { get; }

    private ReadingStatus(int id, string name, bool isPending) : base(id, name) => IsPending = isPending;
}
