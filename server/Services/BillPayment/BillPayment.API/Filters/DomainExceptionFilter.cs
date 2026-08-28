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

        if (context.Exception is InvalidOperationException invalidOp)
        {
            // Warning: vira 400 para quem chamou, mas a origem é a orquestração da Application —
            // vale olhar, ao contrário da recusa de domínio.
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(
                    invalidOp,
                    "----- Application refused: {Message} - Path: {Path} -----",
                    invalidOp.Message,
                    context.HttpContext.Request.Path);

            context.Result = new ObjectResult(new { id = "APP.ERR", message = invalidOp.Message })
            {
                StatusCode = 400,
            };
            context.ExceptionHandled = true;
        }
    }
}
