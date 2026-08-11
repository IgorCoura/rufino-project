namespace BillPayment.Application.Models.CaptureSources;

using System.Text.Json.Serialization;
using BillPayment.Application.CaptureSources.Commands;

/// <summary>
/// Modelos HTTP existem porque o <c>tenantId</c> vem da rota, não do corpo — mandar o tenant no
/// body abriria caminho para divergir do path e virar IDOR.
/// </summary>
/// <remarks>
/// <c>Credential</c> entra pelo corpo e <strong>nunca sai por nenhuma resposta</strong>: o
/// contrato de leitura devolve só a existência da credencial, jamais o valor nem o ponteiro do
/// cofre (ADR-009). O formato desse campo é do adapter do <c>Kind</c> escolhido — a Application
/// o trata como opaco e apenas o guarda.
/// </remarks>
public sealed record ConnectCaptureSourceModel(
    [property: JsonRequired] string Kind,
    [property: JsonRequired] string DisplayName,
    [property: JsonRequired] string Address,
    [property: JsonRequired] string Credential,

    /// <summary>
    /// Pasta a monitorar; ausente = a caixa de entrada inteira. Apontar para uma pasta
    /// alimentada por regra do cliente de e-mail e o que impede o sistema de armazenar
    /// documento pessoal junto com boleto numa caixa de uso misto.
    /// </summary>
    string? FolderPath)
{
    public ConnectCaptureSourceCommand ToCommand(Guid tenantId)
        => new(tenantId, Kind, DisplayName, Address, Credential, FolderPath);
}

public sealed record RenameCaptureSourceModel([property: JsonRequired] string DisplayName)
{
    public RenameCaptureSourceCommand ToCommand(Guid tenantId, Guid captureSourceId)
        => new(tenantId, captureSourceId, DisplayName);
}

public sealed record AlterCaptureSourceActivationModel([property: JsonRequired] bool IsEnabled)
{
    public AlterCaptureSourceActivationCommand ToCommand(Guid tenantId, Guid captureSourceId)
        => new(tenantId, captureSourceId, IsEnabled);
}

/// <summary>
/// Nulo ou ausente devolve a fonte para a caixa de entrada inteira.
/// </summary>
public sealed record ChangeCaptureSourceFolderModel(string? FolderPath)
{
    public ChangeCaptureSourceFolderCommand ToCommand(Guid tenantId, Guid captureSourceId)
        => new(tenantId, captureSourceId, FolderPath);
}

public sealed record ReplaceCaptureSourceCredentialModel([property: JsonRequired] string Credential)
{
    public ReplaceCaptureSourceCredentialCommand ToCommand(Guid tenantId, Guid captureSourceId)
        => new(tenantId, captureSourceId, Credential);
}
