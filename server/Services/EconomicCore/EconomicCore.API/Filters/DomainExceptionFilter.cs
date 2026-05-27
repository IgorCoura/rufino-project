namespace EconomicCore.API.Filters;

using EconomicCore.Domain.SeedWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class DomainExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is DomainException domainEx)
        {
            context.Result = new ObjectResult(new { id = domainEx.Id, message = domainEx.Message })
            {
                StatusCode = domainEx.Category.Id,
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is InvalidOperationException invalidOp)
        {
            context.Result = new ObjectResult(new { id = "APP.ERR", message = invalidOp.Message })
            {
                StatusCode = 400,
            };
            context.ExceptionHandled = true;
        }
    }
}
