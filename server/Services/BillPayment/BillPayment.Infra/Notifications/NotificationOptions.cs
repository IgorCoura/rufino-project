namespace BillPayment.Infra.Notifications;

/// <summary>
/// Canal externo de aviso. Sem configuração, o aviso continua indo para o log — e o alerta
/// continua registrado no agregado, visível no painel de pendências.
/// </summary>
/// <remarks>
/// <strong>É credencial de INSTALAÇÃO, ao contrário da leitura de caixa.</strong> Ler a caixa de
/// um cliente usa o registro de aplicativo do próprio cliente, guardado cifrado por fonte
/// (ADR-006); enviar aviso parte de nós para o cliente, então o remetente é nosso e a credencial
/// é uma só. O segredo vai por variável de ambiente ou <c>user-secrets</c>, nunca no
/// <c>appsettings.json</c> (ADR-009).
/// </remarks>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>
    /// Liga o envio externo. <strong>Desligado por padrão</strong>: sem remetente configurado o
    /// adapter só produziria falha registrada a cada alerta, afogando a falha de verdade.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>O tenant do Entra ID em que o aplicativo remetente está registrado.</summary>
    public string DirectoryId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    /// <summary>NUNCA no <c>appsettings.json</c>. Variável de ambiente ou user-secrets.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// A caixa que assina o aviso. Exige a permissão de aplicativo <c>Mail.Send</c>, restrita por
    /// Application Access Policy a esta caixa — sem a política, <c>Mail.Send</c> permite enviar
    /// como qualquer pessoa do diretório.
    /// </summary>
    public string SenderAddress { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// A base do caminho que o aviso oferece — o que transforma alerta em ação de um clique.
    /// Vazio deixa o aviso sem link, que é degradação e não falha.
    /// </summary>
    public string AppBaseUrl { get; set; } = string.Empty;

    public bool IsConfigured
        => Enabled
            && !string.IsNullOrWhiteSpace(DirectoryId)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret)
            && !string.IsNullOrWhiteSpace(SenderAddress);
}
