using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Net;

namespace PeopleManagement.API.Filters
{
    public class ApplicationExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is DbUpdateException)
            {
                var error = GetDbError(context.Exception);
                if (error != null)
                {
                    context.Exception = new DomainException(nameof(DbUpdateException), error);
                }
            }

            if (context.Exception is DomainException)
            {
                var exception = context.Exception as DomainException;
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Result = new JsonResult(new
                {
                    exception?.Errors
                });
            }
        }

        private static Error? GetDbError(Exception exception)
        {
            var message = exception.InnerException?.Message;
            if (message == null)
                return null;
            if(exception is UniqueConstraintException)
            {
                return InfraErrors.Db.UniqueConstraint(message);
            }
                
            if(exception is CannotInsertNullException)
            {
                return InfraErrors.Db.CannotInsertNull(message);
            }

            if(exception is MaxLengthExceededException) 
            { 
                return InfraErrors.Db.MaxLengthExceeded(message);
            }

            if(exception is NumericOverflowException)
            {
                return InfraErrors.Db.NumericOverflow(message);
            }
                
            if(exception is ReferenceConstraintException)
            {
                return InfraErrors.Db.ReferenceConstraint(message);
            }

            return null;
        }
    }
}
