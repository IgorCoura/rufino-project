﻿using BuildManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace BuidManagement.Api.Filters
{
    public class ApplicationExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is BadRequestException)
            {
                var exception = context.Exception as BadRequestException;
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Result = new JsonResult(new
                {
                    Success = false,
                    Errors = exception?.Errors.Select(x => new
                    {
                        ErrorCode = x.Code,
                        ErrorMessage = x.Msg
                    })
                });
            }

        }
    }
}
