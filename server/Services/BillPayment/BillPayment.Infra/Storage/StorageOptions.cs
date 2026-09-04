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
    /// Região usada para assinar a requisição (SigV4). <strong>Não tem default de propósito.</strong>
    /// </summary>
    /// <remarks>
    /// Serviço auto-hospedado não fica em região da AWS, mas o SigV4 assina com uma mesmo assim —
    /// e o servidor recusa a assinatura quando a região não é a que ele espera. O Garage usa
    /// <c>garage</c> (é o que o BC <c>PeopleManagement</c> configura contra o mesmo servidor); o
    /// MinIO e a maioria dos compatíveis usam <c>us-east-1</c>. Qualquer default aqui estaria
    /// errado para metade dos alvos e a falha apareceria só em runtime, como
    /// <c>SignatureDoesNotMatch</c> ao gravar o primeiro anexo — por isso o valor entra em
    /// <see cref="IsConfigured"/> e a ausência desliga o armazenamento em vez de adivinhar.
    /// </remarks>
    public string? AuthenticationRegion { get; set; }

    /// <summary>
    /// Sem endpoint, credencial e região configurados entra o substituto que falha em toda
    /// operação — e a falha barulhenta é o ponto: guardar em lugar nenhum sem avisar faria o
    /// sistema pagar boleto cujo original ninguém consegue mais recuperar.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceUrl)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(AuthenticationRegion);
}
