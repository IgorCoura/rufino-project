namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Por onde o documento entrou. Espelha <c>CaptureSourceKind</c>, mas é do <c>Bill</c>: a
/// fonte pode ser desativada ou apagada e a procedência do boleto não pode mudar por isso.
/// </summary>
public sealed class BillSourceKind : Enumeration
{
    /// <summary>Caixa de e-mail monitorada via Microsoft Graph. Gmail chega aqui por encaminhamento (ADR-006).</summary>
    public static readonly BillSourceKind Mailbox = new(
        1, "Mailbox", requiresCaptureSource: true, requiresOriginIdentifier: true);

    /// <summary>Portal de concessionária, acessado com credencial do tenant (ADR-012).</summary>
    public static readonly BillSourceKind Portal = new(
        2, "Portal", requiresCaptureSource: true, requiresOriginIdentifier: true);

    /// <summary>Upload feito à mão pelo próprio usuário.</summary>
    public static readonly BillSourceKind ManualUpload = new(
        3, "ManualUpload", requiresCaptureSource: false, requiresOriginIdentifier: false);

    /// <summary>
    /// Se a origem tem que apontar para uma <c>CaptureSource</c> cadastrada. Upload manual
    /// não tem — e é justamente por isso que ele <strong>não</strong> dispensa o check de
    /// pagador: importar à mão não prova que a conta é sua (ADR-004).
    /// </summary>
    public bool RequiresCaptureSource { get; }

    /// <summary>
    /// Se a origem tem que trazer ao menos um rastro externo — fonte, remetente, mensagem,
    /// arquivo ou hash.
    /// </summary>
    /// <remarks>
    /// Verdadeiro para o que entrou sozinho: a captura reconstrói o caminho até a caixa ou o
    /// portal, e sem nenhum ponteiro a evidência seria só o carimbo de data, que não distingue
    /// um documento de outro. <strong>Falso para <see cref="ManualUpload"/></strong>, onde a
    /// identidade é o próprio instrumento digitado — ele é único, sustenta o
    /// <c>Bill.DedupKey</c>, e uma pessoa que cola a linha digitável não tem arquivo, remetente
    /// nem mensagem a oferecer. Exigir um deles ali recusava <strong>toda</strong> importação
    /// manual, que é o caminho que a tela de boletos oferece.
    /// </remarks>
    public bool RequiresOriginIdentifier { get; }

    private BillSourceKind(int id, string name, bool requiresCaptureSource, bool requiresOriginIdentifier)
        : base(id, name)
    {
        RequiresCaptureSource = requiresCaptureSource;
        RequiresOriginIdentifier = requiresOriginIdentifier;
    }
}
