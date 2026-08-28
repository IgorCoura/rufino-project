namespace BillPayment.API.Controllers;

using System.Security.Claims;
using BillPayment.Application.Mediator;
using Microsoft.AspNetCore.Mvc;

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
    /// <remarks>
    /// O payload é destrinchado por <c>{@Command}</c> e por isso passa por <see cref="Loggable"/>:
    /// credencial de cofre e instrumento de pagamento nunca entram no log (ADR-009).
    /// </remarks>
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
    /// Registra que alguém abriu o documento original de um recurso.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Leitura que merece log porque o documento é prova.</strong> Ele é o comprovante do
    /// que o sistema viu quando decidiu pagar, e a trilha já grava quem reivindicou e quem
    /// aprovou — não gravar quem consultou deixaria um buraco no meio dessa mesma trilha.
    /// </para>
    /// <para>
    /// Saem IDs e o usuário, nunca conteúdo, nome de arquivo ou tipo de mídia: o log serve para
    /// dizer <em>quem viu o quê</em>, não para reconstituir o documento fora do cofre.
    /// </para>
    /// </remarks>
    protected void ArtifactAccessLog(Guid tenantId, string resource, Guid resourceId)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
            return;

        _logger.LogInformation(
            "----- Artifact served: {Resource} - Id: {ResourceId} - Tenant: {TenantId} - User: {UserId} -----",
            resource,
            resourceId,
            tenantId,
            ResolveDecidingUserId());
    }

    /// <summary>
    /// Quem está decidindo. Vem do <c>sub</c> do token, e de nenhum outro lugar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Havia um fallback pelo header <c>x-user-id</c>, e ele morreu quando o Keycloak
    /// entrou.</strong> Enquanto não havia token, o header ERA a identidade — qualquer um que
    /// alcançasse a API escolhia em nome de quem aprovar, e o ADR-007 apoia toda a trilha de
    /// autorização de pagamento nesse campo. Não recoloque o parâmetro "para as ferramentas":
    /// os scripts precisam de token de qualquer forma, porque quem os barra é a autenticação.
    /// </para>
    /// <para>
    /// Devolve <c>Guid.Empty</c> quando o token não traz <c>sub</c> utilizável: quem recusa é o
    /// domínio (<c>BLP.BIL22</c>), para a regra viver num lugar só — lançar daqui trocaria uma
    /// recusa de negócio por um 500.
    /// </para>
    /// </remarks>
    protected Guid ResolveDecidingUserId()
        => TryGetUserId(out var fromToken) ? fromToken : Guid.Empty;

    // Idempotência permissiva: header x-requestid ausente (Guid.Empty) gera um novo Id por request,
    // de modo que cada chamada sem header é tratada como intenção distinta (nunca colide na tabela).
    protected static Guid EnsureRequestId(Guid requestId)
        => requestId == Guid.Empty ? Guid.NewGuid() : requestId;
}
