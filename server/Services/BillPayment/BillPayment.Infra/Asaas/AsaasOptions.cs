namespace BillPayment.Infra.Asaas;

/// <summary>
/// Configuração do provedor de consulta e pagamento de contas.
/// </summary>
/// <remarks>
/// <strong><see cref="ApiKey"/> nunca vem de <c>appsettings.json</c>.</strong> Ela chega por
/// variável de ambiente (<c>Asaas__ApiKey</c>, injetada pelo Dokploy) ou por
/// <c>dotnet user-secrets</c> em desenvolvimento e testes — regra do <c>ADR-009</c>. E o
/// segredo é perigoso desde a Fase 1: a consulta exige permissão de saque na chave, então essa
/// string paga contas se vazar.
/// </remarks>
public sealed class AsaasOptions
{
    public const string SectionName = "Asaas";

    /// <summary>Sandbox por padrão — apontar para produção é decisão explícita de quem configura.</summary>
    public string BaseUrl { get; set; } = "https://api-sandbox.asaas.com/v3/";

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Sem chave, os adapters de consulta são substituídos por versões que devolvem
    /// <c>Unavailable</c>. É o que permite subir a aplicação e rodar a suíte de integração sem
    /// credencial — e o que impede que a ausência dela seja confundida com "documento suspeito".
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
