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
    string? FolderPath,

    /// <summary>
    /// Piso temporal: nada recebido antes desta data é lido. Ausente = a caixa inteira.
    /// </summary>
    /// <remarks>
    /// Quem aplica o corte é o provedor — a delta query do Graph aceita exatamente
    /// <c>receivedDateTime ge {data}</c>. Sem ele, conectar uma caixa antiga varre anos de
    /// histórico na primeira sincronização.
    /// </remarks>
    DateOnly? CaptureSince)
{
    public ConnectCaptureSourceCommand ToCommand(Guid tenantId)
        => new(tenantId, Kind, DisplayName, Address, Credential, FolderPath, CaptureSince);
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

/// <summary>
/// Move o piso temporal da captura. Nulo ou ausente devolve a fonte à caixa inteira.
/// </summary>
/// <remarks>
/// Trocar a data <strong>descarta os cursores</strong> de todas as pastas — o provedor grava o
/// filtro dentro do cursor, e sem o descarte a data nova não valeria nada. A releitura não
/// duplica: a ingestão é idempotente por <c>(tenant, fonte, mensagem, anexo)</c>.
/// </remarks>
public sealed record ChangeCaptureSourceSinceModel(DateOnly? CaptureSince)
{
    public ChangeCaptureSourceSinceCommand ToCommand(Guid tenantId, Guid captureSourceId)
        => new(tenantId, captureSourceId, CaptureSince);
}

public sealed record ReplaceCaptureSourceCredentialModel([property: JsonRequired] string Credential)
{
    public ReplaceCaptureSourceCredentialCommand ToCommand(Guid tenantId, Guid captureSourceId)
        => new(tenantId, captureSourceId, Credential);
}

/// <summary>
/// Acrescenta uma pasta à lista acompanhada. Nulo ou ausente = a caixa de entrada.
/// </summary>
/// <remarks>
/// O caminho vai no <strong>corpo</strong>, não na rota: nome de pasta é texto livre e contém
/// <c>/</c> por definição — no path viraria outro segmento e a requisição morreria em 404 de
/// roteamento, antes de chegar ao controller.
/// </remarks>
public sealed record AddCaptureSourceFolderModel(string? FolderPath)
{
    public AddCaptureSourceFolderCommand ToCommand(Guid tenantId, Guid captureSourceId)
        => new(tenantId, captureSourceId, FolderPath);
}
