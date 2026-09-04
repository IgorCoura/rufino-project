namespace BillPayment.Infra.Mailboxes.Graph;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// O que a fonte guarda no cofre para falar com o Graph: o trio de <em>client credentials</em>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Este é o formato do campo <c>credential</c> da API</strong>, e ele é contrato do
/// adapter — a Application guarda a string opaca e nunca a interpreta. Um JSON com três campos:
/// </para>
/// <code>
/// {"directoryId":"&lt;guid do tenant no Entra ID&gt;","clientId":"&lt;guid do app&gt;","clientSecret":"&lt;segredo&gt;"}
/// </code>
/// <para>
/// O cliente cria o registro no <em>próprio</em> Entra ID (ADR-006), concede a permissão de
/// aplicativo <c>Mail.Read</c> e a restringe por <strong>Application Access Policy</strong> ao
/// grupo com as caixas monitoradas. Sem essa política, <c>Mail.Read</c> alcança todas as caixas
/// do tenant — é a diferença entre ler uma caixa e ler a empresa inteira.
/// </para>
/// <para>
/// <strong>Nada aqui vai para log, exceção ou resposta de API.</strong> <see cref="ToString"/> é
/// sobrescrito porque o <c>record</c> imprimiria o segredo em qualquer interpolação distraída.
/// </para>
/// </remarks>
internal sealed record GraphMailboxCredential(
    [property: JsonPropertyName("directoryId")] string DirectoryId,
    [property: JsonPropertyName("clientId")] string ClientId,
    [property: JsonPropertyName("clientSecret")] string ClientSecret)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static bool TryParse(string? raw, out GraphMailboxCredential? credential)
    {
        credential = null;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<GraphMailboxCredential>(raw, Json);

            if (parsed is null
                || string.IsNullOrWhiteSpace(parsed.DirectoryId)
                || string.IsNullOrWhiteSpace(parsed.ClientId)
                || string.IsNullOrWhiteSpace(parsed.ClientSecret))
            {
                return false;
            }

            credential = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Identidade da credencial para o cache de token — o trio inteiro, reduzido a um hash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O segredo PRECISA participar da chave.</strong> <c>directoryId</c> e
    /// <c>clientId</c> são públicos — o primeiro sai do <c>.well-known/openid-configuration</c>,
    /// o segundo qualquer membro do diretório enxerga no portal. Uma chave só com os dois
    /// entregava o token quente de uma conta a quem apresentasse o par certo com QUALQUER
    /// segredo: outro tenant conectando a mesma caixa com credencial inválida passava na prova de
    /// acesso e lia a caixa do primeiro. Foi o achado mais grave da auditoria de 2026-08-28.
    /// </para>
    /// <para>
    /// Entra como SHA-256, não em claro: a chave vive num dicionário em memória, e o segredo em
    /// texto ali se espalharia por dump e por qualquer diagnóstico que imprimisse o cache.
    /// </para>
    /// </remarks>
    public string CacheKey => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes($"{DirectoryId}\n{ClientId}\n{ClientSecret}")));

    public override string ToString() => $"GraphMailboxCredential {{ DirectoryId = {DirectoryId}, ClientId = {ClientId} }}";
}
