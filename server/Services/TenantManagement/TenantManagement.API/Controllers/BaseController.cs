namespace TenantManagement.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TenantManagement.Application.Mediator;

[ApiController]
public class BaseController(ILogger<BaseController> logger) : ControllerBase
{
    private readonly ILogger<BaseController> _logger = logger;

    protected ActionResult OkResponse(object? result = null)
    {
        return Ok(result);
    }

    protected Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub))
            throw new UnauthorizedAccessException("Token bearer não contém o claim 'sub'.");

        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Claim 'sub' do token não é um GUID válido.");

        return userId;
    }

    protected bool TryGetUserId(out Guid userId)
    {
        try
        {
            userId = GetUserId();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            userId = Guid.Empty;
            return false;
        }
    }

    // Substitui o payload de um ISensitiveCommand. Não é "[REDACTED]" genérico de propósito: a linha
    // precisa dizer que a omissão foi decidida, senão ela lê como campo que faltou.
    private const string SENSITIVE_PAYLOAD = "[omitido: ISensitiveCommand]";

    /// <summary>
    /// Registra o Command imediatamente antes do despacho. Sai o nome do Command, o id da rota e o
    /// <c>x-requestid</c> efetivo — os três que permitem correlacionar esta linha com a do resultado,
    /// com o <c>LoggingBehavior</c> e com o <c>IdentifiedCommandHandler</c>.
    /// </summary>
    protected void SendingCommandLog(object? commandId, object? command, Guid requestId)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
            return;

        _logger.LogInformation(
            "----- Sending command: {CommandName} - Id: {CommandId} - RequestId: {RequestId} ({@Command}) -----",
            command?.GetType().Name,
            commandId,
            requestId,
            Loggable(command));
    }

    /// <summary>
    /// Registra o desfecho do despacho. O payload NÃO se repete aqui — ele já saiu na linha de envio
    /// e as duas se correlacionam pelo <c>RequestId</c>; repeti-lo dobraria o volume e a superfície de
    /// vazamento sem acrescentar informação.
    /// </summary>
    protected void CommandResultLog(object? result, object? commandId, object? command, Guid requestId)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
            return;

        _logger.LogInformation(
            "----- Command result: {@Result} - {CommandName} - Id: {CommandId} - RequestId: {RequestId} -----",
            result,
            command?.GetType().Name,
            commandId,
            requestId);
    }

    private static object? Loggable(object? command)
        => command is ISensitiveCommand ? SENSITIVE_PAYLOAD : command;

    /// <summary>
    /// Quem está decidindo. Prefere o <c>sub</c> do token; sem token, cai para o header.
    /// </summary>
    /// <remarks>
    /// <strong>O fallback por header é provisório e morre na fase 6.</strong> Nesta fase não há
    /// Keycloak configurado e o <c>User</c> chega sem claims, então sem ele não haveria como
    /// registrar quem aprovou — e o ADR-007 exige um <c>UserId</c> em todo pagamento. Quando o
    /// token entrar, o caminho do claim vence sozinho e este método vira uma linha a apagar.
    /// <para>
    /// Devolve <c>Guid.Empty</c> quando nada identifica o usuário: quem recusa é o domínio
    /// (<c>BLP.BIL22</c>), para a regra viver num lugar só.
    /// </para>
    /// </remarks>
    protected Guid ResolveDecidingUserId(Guid headerUserId)
        => TryGetUserId(out var fromToken) ? fromToken : headerUserId;

    // Idempotência permissiva: header x-requestid ausente (Guid.Empty) gera um novo Id por request,
    // de modo que cada chamada sem header é tratada como intenção distinta (nunca colide na tabela).
    protected static Guid EnsureRequestId(Guid requestId)
        => requestId == Guid.Empty ? Guid.NewGuid() : requestId;
}
