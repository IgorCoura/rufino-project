namespace BillPayment.Domain.CapturedMessages;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que a captura fez com um artefato — <strong>na linguagem de quem opera</strong>, não na do
/// funil.
/// </summary>
/// <remarks>
/// <para>
/// Espelha o desfecho do <c>CaptureItem</c>, com um valor que o <c>CaptureItemStatus</c> não tem
/// e nunca poderia ter: <see cref="Discarded"/>. Artefato descartado não deixa item — a linha é
/// apagada —, e é justamente esse o caso em que uma pessoa fica sem saber o que aconteceu com o
/// e-mail que ela mandou.
/// </para>
/// <para>
/// Não são os dez estados do funil: <c>Received</c>, <c>Parsed</c> e <c>LinkPending</c> são
/// passagem, e listá-los aqui obrigaria a tela a explicar mecânica interna. O que está em
/// trânsito é <see cref="Pending"/>.
/// </para>
/// </remarks>
public sealed class ArtifactOutcome : Enumeration
{
    /// <summary>Ainda não processado, ou em trânsito pelo funil.</summary>
    public static readonly ArtifactOutcome Pending = new(1, nameof(Pending));

    /// <summary>Virou boleto deste cliente.</summary>
    public static readonly ArtifactOutcome Promoted = new(2, nameof(Promoted), producesBill: true);

    /// <summary>Boleto sem dono determinado — fila de reivindicação.</summary>
    public static readonly ArtifactOutcome Unrouted = new(3, nameof(Unrouted));

    /// <summary>O documento diz, sob rótulo, que o pagador é outro.</summary>
    public static readonly ArtifactOutcome ForeignPayer = new(4, nameof(ForeignPayer));

    /// <summary>Nenhum boleto reconhecido, mas o remetente é cadastrado — ficou para revisão.</summary>
    public static readonly ArtifactOutcome Quarantined = new(5, nameof(Quarantined));

    /// <summary>PDF que nenhuma senha derivada abriu.</summary>
    public static readonly ArtifactOutcome Locked = new(6, nameof(Locked));

    /// <summary>O provedor não entregou o arquivo.</summary>
    public static readonly ArtifactOutcome DownloadFailed = new(7, nameof(DownloadFailed));

    /// <summary>
    /// Não era boleto e o remetente não é cadastrado: o item foi apagado e o arquivo, nunca
    /// guardado. <strong>Só este registro sabe que existiu.</strong>
    /// </summary>
    public static readonly ArtifactOutcome Discarded = new(8, nameof(Discarded));

    /// <summary>O processamento estourou e desistiu — não se chegou a saber o que o artefato é.</summary>
    /// <remarks>
    /// <strong>Não se confunde com <see cref="DownloadFailed"/></strong>: lá o arquivo não veio,
    /// aqui ele veio e a leitura é que não fechou. E menos ainda com
    /// <see cref="Quarantined"/>, que é uma resposta sobre o documento ("não achei boleto");
    /// este é uma resposta sobre o sistema. A distinção decide o que a pessoa faz a seguir:
    /// digitar a linha à mão, ou avisar que algo está quebrado.
    /// <para>
    /// Antes de existir, o artefato nessa situação ficava em <see cref="Pending"/>
    /// indefinidamente — o e-mail aparecia como "ainda não processado" para sempre, que é
    /// exatamente o sintoma relatado em 2026-08-26.
    /// </para>
    /// </remarks>
    public static readonly ArtifactOutcome ProcessingFailed = new(9, nameof(ProcessingFailed));

    /// <summary>
    /// A mensagem não trouxe nada para processar — nem anexo aproveitável, nem sinal no corpo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o único valor que nunca aparece numa linha de anexo</strong>: ele descreve a
    /// mensagem, e é calculado justamente porque não há anexo nenhum de onde derivar desfecho.
    /// </para>
    /// <para>
    /// <strong>Existe porque a ausência estava sendo lida como espera.</strong> Desde que a
    /// mensagem sem anexo passou a entrar no livro-caixa — para quem mandou o e-mail ter
    /// resposta —, o desfecho dominante dela caía no fallback <see cref="Pending"/> e a tela
    /// dizia "Na fila" para sempre. Medido em 2026-08-26: <strong>23 de 39 mensagens</strong>
    /// da caixa real, todas propaganda e notificação, eternamente "na fila" sem que houvesse
    /// fila nenhuma. O sistema estava certo e a frase é que mentia.
    /// </para>
    /// </remarks>
    public static readonly ArtifactOutcome NothingToProcess = new(10, nameof(NothingToProcess));

    /// <summary>Uma pessoa olhou e disse que não reconhece — saiu da fila de pendências.</summary>
    /// <remarks>
    /// <strong>Não é <see cref="Discarded"/></strong>: aquele é o sistema reconhecendo duplicata,
    /// este é uma decisão humana com autor registrado. Quem lê a tela precisa distinguir "o
    /// sistema jogou fora" de "alguém disse que não é nosso" — a segunda é reversível e a
    /// primeira não descreve escolha nenhuma.
    /// </remarks>
    public static readonly ArtifactOutcome Dismissed = new(11, nameof(Dismissed));

    /// <summary>Se este desfecho produziu um boleto — o que torna o registro inpurgável.</summary>
    public bool ProducesBill { get; }

    private ArtifactOutcome(int id, string name, bool producesBill = false) : base(id, name)
    {
        ProducesBill = producesBill;
    }
}
