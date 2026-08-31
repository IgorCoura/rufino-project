namespace BillPayment.Infra.Asaas;

using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using Microsoft.Extensions.Logging;

/// <summary>
/// Resolve a credencial DO TENANT no cofre e monta o <see cref="HttpClient"/> daquela chamada —
/// molde do <c>GraphMailboxReader.AuthenticateAsync</c>: a Infra resolve o ponteiro no início de
/// cada operação, e a falha vira motivo modelado, nunca exceção.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não há chave global de fallback, por decisão do usuário (2026-08-31).</strong>
/// Tenant sem chave própria fica com a consulta indisponível — usar a chave da instalação em
/// nome dele é exatamente o que o doc 07 mandou desmontar, porque é a chave que paga.
/// </para>
/// <para>
/// O <c>CreateClient</c> devolve uma instância nova por chamada sobre o handler compartilhado
/// (com a resiliência do registro), então o header <c>access_token</c> preenchido aqui vive só
/// naquela chamada — nunca num cliente compartilhado entre tenants.
/// </para>
/// </remarks>
internal sealed class AsaasClientProvider(
    IHttpClientFactory httpClientFactory,
    ISecretVault vault,
    ILogger<AsaasClientProvider> logger)
{
    public const string TENANT_KEY_NOT_CONFIGURED = "tenant_key_not_configured";
    public const string CREDENTIAL_UNRESOLVABLE = "credential_unresolvable";

    public const string TENANT_KEY_NOT_CONFIGURED_MESSAGE =
        "Consulta oficial indisponível: configure a chave Asaas do tenant no Perfil do Pagador.";

    private const string CREDENTIAL_UNRESOLVABLE_MESSAGE =
        "Consulta oficial indisponível: a credencial do tenant não pôde ser lida no cofre.";

    /// <summary>
    /// Cliente pronto para a chamada, ou o motivo da degradação. Quem chama traduz o motivo
    /// para o <c>Unavailable</c> do seu tipo de resultado — ambos os motivos são retentáveis
    /// do ponto de vista do documento: nada foi aprendido sobre ele.
    /// </summary>
    public async Task<(HttpClient? Client, string? ReasonCode, string? Message)> CreateForAsync(
        CredentialRef? credential,
        CancellationToken cancellationToken)
    {
        if (credential is null)
            return (null, TENANT_KEY_NOT_CONFIGURED, TENANT_KEY_NOT_CONFIGURED_MESSAGE);

        string apiKey;
        try
        {
            apiKey = await vault.ResolveAsync(credential, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // O detalhe fica no log; o motivo que viaja não pode carregar nada do cofre.
            logger.LogError(ex, "Não foi possível resolver a chave Asaas do tenant no cofre");
            return (null, CREDENTIAL_UNRESOLVABLE, CREDENTIAL_UNRESOLVABLE_MESSAGE);
        }

        var client = httpClientFactory.CreateClient(AsaasHttp.LOOKUP_CLIENT_NAME);
        client.DefaultRequestHeaders.Add("access_token", apiKey);
        return (client, null, null);
    }
}
