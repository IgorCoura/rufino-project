namespace BillPayment.API.Filters;

using BillPayment.Domain.SeedWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Traduz exceção em resposta — e registra o que traduziu.
/// </summary>
/// <remarks>
/// O registro existe porque marcar <c>ExceptionHandled</c> tira a exceção do caminho do middleware
/// do ASP.NET Core, que é quem logaria: sem estas linhas, tudo o que este filtro trata desaparece
/// do log, e o par <c>SendingCommandLog</c>/<c>CommandResultLog</c> do <c>BaseController</c> fica com
/// a linha de envio sem desfecho nenhum — um log que mente por omissão.
/// <para>
/// Exceção <strong>inesperada</strong> não é tratada aqui de propósito: ela segue para o middleware,
/// que já a registra em <c>Error</c>. Capturá-la para logar duplicaria a entrada e trocaria a página
/// de diagnóstico do ambiente de desenvolvimento por um 500 opaco.
/// </para>
/// </remarks>
public sealed class DomainExceptionFilter(ILogger<DomainExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Exception is DomainException domainEx)
        {
            // Information, não Error: regra de negócio recusando é o sistema funcionando.
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "----- Domain rule refused: {ErrorId} - {Message} - Status: {StatusCode} - Path: {Path} -----",
                    domainEx.Id,
                    domainEx.Message,
                    domainEx.Category.Id,
                    context.HttpContext.Request.Path);

            context.Result = new ObjectResult(new { id = domainEx.Id, message = domainEx.Message })
            {
                StatusCode = domainEx.Category.Id,
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is ConcurrencyConflictException conflict)
        {
            // Duas pessoas decidiram sobre o mesmo registro ao mesmo tempo: a segunda perde e
            // precisa recarregar. Information, como a recusa de domínio — é o sistema protegendo
            // a decisão da primeira, não um defeito.
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "----- Concurrency conflict: {Message} - Path: {Path} -----",
                    conflict.Message,
                    context.HttpContext.Request.Path);

            context.Result = new ObjectResult(new { id = "APP.CONCURRENCY", message = conflict.Message })
            {
                StatusCode = StatusCodes.Status409Conflict,
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is EnumerationNotFoundException invalidInput)
        {
            // SÓ o Smart Enum que não casou vira 400 — é a tradução de input do handler
            // ("SourceKind", "Kind"). Até 2026-08-28 qualquer InvalidOperationException entrava
            // aqui, inclusive as internas do EF Core, com a mensagem crua no corpo da resposta.
            // As demais seguem para o middleware, que as registra em Error e devolve 500 opaco.
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(
                    invalidInput,
                    "----- Application refused: {Message} - Path: {Path} -----",
                    invalidInput.Message,
                    context.HttpContext.Request.Path);

            context.Result = new ObjectResult(new { id = "APP.INPUT", message = invalidInput.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is FileNotFoundException)
        {
            // Artefato que não existe — ou que não pertence a este tenant, que para quem pergunta
            // é a mesma coisa (doutrina do 404 uniforme do ADR-008). Sem o nome do arquivo.
            context.Result = new ObjectResult(new { id = "APP.NOT_FOUND", message = "Documento não encontrado." })
            {
                StatusCode = StatusCodes.Status404NotFound,
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is UnauthorizedAccessException unauthenticated)
        {
            // O token passou na assinatura mas não traz um `sub` utilizável: é problema de
            // autenticação, não de autorização — 401, e nunca 500.
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(
                    "----- Unusable identity: {Message} - Path: {Path} -----",
                    unauthenticated.Message,
                    context.HttpContext.Request.Path);

            context.Result = new ObjectResult(new { id = "APP.UNAUTHENTICATED", message = unauthenticated.Message })
            {
                StatusCode = StatusCodes.Status401Unauthorized,
            };
            context.ExceptionHandled = true;
        }
    }
}
