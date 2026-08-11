namespace BillPayment.Infra.Persistence;

/// <summary>
/// Uma credencial de tenant cifrada. Infraestrutura, não Aggregate — o Domain só conhece a
/// referência (<c>CredentialRef</c>) e nunca esta linha.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Envelope encryption.</strong> Cada segredo tem um DEK próprio de 256 bits; o payload
/// é cifrado com esse DEK em AES-256-GCM, e o DEK é cifrado com a master key vinda da variável
/// de ambiente. Trocar a master key re-envelopa DEKs, sem tocar em nenhum payload.
/// </para>
/// <para>
/// <strong><see cref="KekVersion"/> existe desde a primeira linha</strong>, mesmo havendo só uma
/// versão. Acrescentá-la depois exigiria adivinhar com qual chave cada linha antiga foi cifrada
/// — que é exatamente a informação que ela guarda.
/// </para>
/// <para>
/// <c>TenantId</c> e <c>Kind</c> entram no dado autenticado da cifra (AAD). Não são só colunas
/// de busca: adulterar qualquer uma delas faz a decifragem falhar em vez de devolver o segredo
/// de outro tenant.
/// </para>
/// </remarks>
public sealed class TenantSecret
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public int Kind { get; private set; }
    public int KekVersion { get; private set; }

    public byte[] WrappedDek { get; private set; } = [];
    public byte[] DekNonce { get; private set; } = [];
    public byte[] DekTag { get; private set; } = [];

    public byte[] Ciphertext { get; private set; } = [];
    public byte[] Nonce { get; private set; } = [];
    public byte[] Tag { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TenantSecret() { }

    internal TenantSecret(Guid id, Guid tenantId, int kind, int kekVersion, DateTimeOffset createdAt)
    {
        Id = id;
        TenantId = tenantId;
        Kind = kind;
        KekVersion = kekVersion;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>
    /// Grava um envelope novo. Sempre substitui <em>todos</em> os campos de uma vez — reaproveitar
    /// nonce entre gravações quebraria o AES-GCM, e é por isso que não existe setter por campo.
    /// </summary>
    internal void SetEnvelope(
        byte[] wrappedDek,
        byte[] dekNonce,
        byte[] dekTag,
        byte[] ciphertext,
        byte[] nonce,
        byte[] tag,
        int kekVersion,
        DateTimeOffset updatedAt)
    {
        WrappedDek = wrappedDek;
        DekNonce = dekNonce;
        DekTag = dekTag;
        Ciphertext = ciphertext;
        Nonce = nonce;
        Tag = tag;
        KekVersion = kekVersion;
        UpdatedAt = updatedAt;
    }
}
