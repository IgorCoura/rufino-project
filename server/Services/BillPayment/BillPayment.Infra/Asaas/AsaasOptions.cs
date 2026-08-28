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

    /// <summary>
    /// Como esta aplicação se identifica ao provedor, e <strong>é obrigatório</strong>: sem
    /// <c>User-Agent</c> o Asaas recusa a requisição antes de olhar o corpo, com 400 e a
    /// descrição "É obrigatório preencher User-Agent no cabeçalho da requisição".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Não é opção de configuração, e isso é deliberado.</strong> O valor identifica a
    /// aplicação, não o ambiente — e um campo configurável poderia chegar vazio, que é
    /// exatamente o estado que quebra. Constante não tem como ser esvaziada por
    /// <c>appsettings</c> nem por variável de ambiente.
    /// </para>
    /// <para>
    /// <strong>O <c>HttpClient</c> do .NET não manda <c>User-Agent</c> por padrão</strong> — ao
    /// contrário do <c>fetch</c> do Node, que manda <c>node</c> sozinho. É por isso que as
    /// sondas de fumaça de 2026-08-06 saíram verdes contra o mesmo endpoint que o adapter não
    /// conseguia chamar: a ferramenta de medição preenchia por conta própria um cabeçalho que a
    /// implementação nunca preencheu.
    /// </para>
    /// </remarks>
    public const string USER_AGENT = "RufinoBillPayment/1.0";

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
