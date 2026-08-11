namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Por onde o documento entrou. Espelha <c>CaptureSourceKind</c>, mas é do <c>Bill</c>: a
/// fonte pode ser desativada ou apagada e a procedência do boleto não pode mudar por isso.
/// </summary>
public sealed class BillSourceKind : Enumeration
{
    /// <summary>Caixa de e-mail monitorada via Microsoft Graph. Gmail chega aqui por encaminhamento (ADR-006).</summary>
    public static readonly BillSourceKind Mailbox = new(1, "Mailbox", requiresCaptureSource: true);

    /// <summary>Portal de concessionária, acessado com credencial do tenant (ADR-012).</summary>
    public static readonly BillSourceKind Portal = new(2, "Portal", requiresCaptureSource: true);

    /// <summary>Upload feito à mão pelo próprio usuário.</summary>
    public static readonly BillSourceKind ManualUpload = new(3, "ManualUpload", requiresCaptureSource: false);

    /// <summary>
    /// Se a origem tem que apontar para uma <c>CaptureSource</c> cadastrada. Upload manual
    /// não tem — e é justamente por isso que ele <strong>não</strong> dispensa o check de
    /// pagador: importar à mão não prova que a conta é sua (ADR-004).
    /// </summary>
    public bool RequiresCaptureSource { get; }

    private BillSourceKind(int id, string name, bool requiresCaptureSource) : base(id, name)
        => RequiresCaptureSource = requiresCaptureSource;
}
