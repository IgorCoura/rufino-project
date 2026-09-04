namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Como terminou uma tentativa de leitura por IA.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque colapsar "não achei" com "não respondi" faz indisponibilidade de rede
/// virar suspeita do documento.</strong> É a mesma lição que o <c>LookupStatus</c> e o
/// <c>MailboxStatus</c> já carregavam — e a porta de IA era a única das três integrações do BC
/// que não a tinha. O preço apareceu medido em 2026-08-27: de 614 chamadas reais, 96 morreram em
/// timeout, 24 em 503 e 48 em 400, e <em>todas</em> chegavam ao chamador como
/// <c>ExtractedDocument.Empty</c> — indistinguíveis de "o modelo leu e não achou boleto". O
/// resultado é que documento bom ia para a quarentena por indisponibilidade do provedor, e a
/// máquina de retentativa que já existia na fila nunca era acionada.
/// </para>
/// <para>
/// <strong>A pergunta que este enum responde é uma só: vale a pena tentar de novo?</strong>
/// </para>
/// </remarks>
public sealed class ExtractionStatus : Enumeration
{
    /// <summary>O modelo respondeu e trouxe candidatos.</summary>
    public static readonly ExtractionStatus Resolved = new(1, nameof(Resolved), hasContent: true, isRetryable: false);

    /// <summary>
    /// O modelo respondeu e não achou nada. <strong>Desfecho legítimo e o mais comum</strong> — o
    /// artefato provavelmente não é boleto. Retentar daria o mesmo, e gastaria cota.
    /// </summary>
    public static readonly ExtractionStatus Empty = new(2, nameof(Empty), hasContent: false, isRetryable: false);

    /// <summary>
    /// O provedor não respondeu — timeout, 5xx, transporte caído. <strong>Nada foi aprendido
    /// sobre o documento</strong>, e é este o caso em que retentar é a resposta certa.
    /// </summary>
    public static readonly ExtractionStatus Unavailable = new(3, nameof(Unavailable), hasContent: false, isRetryable: true);

    /// <summary>
    /// O provedor recusou a requisição (400). É fato sobre o <em>artefato</em>, não sobre a rede:
    /// repetir produz a mesma recusa e queima cota.
    /// </summary>
    public static readonly ExtractionStatus Rejected = new(4, nameof(Rejected), hasContent: false, isRetryable: false);

    /// <summary>
    /// A cota do dia acabou, ou o intervalo mínimo entre chamadas ainda não passou. Retentável —
    /// mas amanhã, não agora; quem espaça é a espera da própria fila.
    /// </summary>
    public static readonly ExtractionStatus BudgetExhausted = new(5, nameof(BudgetExhausted), hasContent: false, isRetryable: true);

    /// <summary>Se há documento lido para o chamador consumir.</summary>
    public bool HasContent { get; }

    /// <summary>
    /// Se tentar de novo pode mudar o desfecho. <strong>Só a indisponibilidade e a cota são
    /// retentáveis</strong> — o resto é fato sobre o documento, e insistir nele gastaria o teto
    /// diário num artefato só.
    /// </summary>
    public bool IsRetryable { get; }

    private ExtractionStatus(int id, string name, bool hasContent, bool isRetryable) : base(id, name)
    {
        HasContent = hasContent;
        IsRetryable = isRetryable;
    }
}
