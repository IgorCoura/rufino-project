namespace BillPayment.Infra.Storage;

/// <summary>
/// Configuração do armazenamento dos artefatos capturados.
/// </summary>
/// <remarks>
/// <strong><see cref="SecretKey"/> nunca vem de <c>appsettings.json</c></strong> — chega por
/// variável de ambiente ou <c>dotnet user-secrets</c>, regra do <c>ADR-009</c>. O alvo é um
/// serviço compatível com S3 auto-hospedado (Garage), não a AWS: a premissa do projeto é
/// software open source sem custo de nuvem.
/// </remarks>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Endpoint do serviço compatível com S3. Vazio desliga o armazenamento.</summary>
    public string? ServiceUrl { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string Bucket { get; set; } = "billpayment-captures";

    /// <summary>
    /// Serviço auto-hospedado usa caminho (<c>host/bucket/chave</c>) em vez de subdomínio, que
    /// exigiria DNS curinga.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Sem endpoint e credencial configurados entra o substituto que falha em toda operação —
    /// e a falha barulhenta é o ponto: guardar em lugar nenhum sem avisar faria o sistema
    /// pagar boleto cujo original ninguém consegue mais recuperar.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceUrl)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey);
}
