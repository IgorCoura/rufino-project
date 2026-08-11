namespace BillPayment.Domain.CaptureSources;

using BillPayment.Domain.SeedWork;

/// <summary>
/// A natureza do que está sendo monitorado — e, por consequência, que forma o endereço tem e
/// se a fonte precisa de credencial.
/// </summary>
/// <remarks>
/// <para>
/// O catálogo inteiro é declarado desde já, pela mesma razão do <c>SecretKind</c>: o id é
/// gravado no banco, e acrescentar um valor depois é barato — mudar o significado de um valor
/// já gravado não é. Só <see cref="MicrosoftGraphMailbox"/> tem adapter na fase 2;
/// <see cref="Portal"/> é fase 5 e <see cref="ManualUpload"/> existe para o dia em que a
/// importação manual precisar aparecer no mesmo painel de fontes.
/// </para>
/// <para>
/// <strong>Não existe Kind para Gmail.</strong> O ADR-006 resolve conta pessoal por
/// encaminhamento para a caixa do Microsoft 365 — do ponto de vista do sistema é a mesma fonte,
/// e o <c>From:</c> original é preservado, então <c>OriginTrust</c> continua olhando o
/// remetente verdadeiro.
/// </para>
/// </remarks>
public sealed class CaptureSourceKind : Enumeration
{
    /// <summary>Caixa de e-mail lida por delta query do Microsoft Graph.</summary>
    public static readonly CaptureSourceKind MicrosoftGraphMailbox = new(
        1,
        nameof(MicrosoftGraphMailbox),
        requiresEmailAddress: true,
        requiresWebUrl: false,
        requiresCredential: true,
        supportsIncrementalSync: true);

    /// <summary>Portal de fornecedor com login (fase 5).</summary>
    public static readonly CaptureSourceKind Portal = new(
        2,
        nameof(Portal),
        requiresEmailAddress: false,
        requiresWebUrl: true,
        requiresCredential: true,
        supportsIncrementalSync: false);

    /// <summary>Upload feito por uma pessoa. Não tem o que sincronizar e não guarda credencial.</summary>
    public static readonly CaptureSourceKind ManualUpload = new(
        3,
        nameof(ManualUpload),
        requiresEmailAddress: false,
        requiresWebUrl: false,
        requiresCredential: false,
        supportsIncrementalSync: false);

    /// <summary>O endereço é uma caixa de e-mail e passa pela sintaxe de <c>EmailSyntax</c>.</summary>
    public bool RequiresEmailAddress { get; }

    /// <summary>O endereço é uma URL absoluta <c>https</c>.</summary>
    public bool RequiresWebUrl { get; }

    /// <summary>A fonte só existe apontando para uma credencial no cofre (<c>BLP.CPS01</c>).</summary>
    public bool RequiresCredential { get; }

    /// <summary>
    /// Existe um leitor capaz de trazer o que mudou desde o cursor. Falso para
    /// <see cref="ManualUpload"/> (nada a varrer) e para <see cref="Portal"/> enquanto a fase 5
    /// não chega — o job pula essas fontes em vez de registrar falha, porque não ter o que
    /// sincronizar não é erro.
    /// </summary>
    public bool SupportsIncrementalSync { get; }

    private CaptureSourceKind(
        int id,
        string name,
        bool requiresEmailAddress,
        bool requiresWebUrl,
        bool requiresCredential,
        bool supportsIncrementalSync)
        : base(id, name)
    {
        RequiresEmailAddress = requiresEmailAddress;
        RequiresWebUrl = requiresWebUrl;
        RequiresCredential = requiresCredential;
        SupportsIncrementalSync = supportsIncrementalSync;
    }
}
