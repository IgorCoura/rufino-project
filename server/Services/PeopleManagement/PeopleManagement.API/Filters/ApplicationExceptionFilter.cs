﻿using Microsoft.AspNetCore.Mvc.Filters;
using PeopleManagement.Domain.Exceptions;
using System.Net;

namespace PeopleManagement.API.Filters
{
    public class ApplicationExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is DomainException)
            {
                var exception = context.Exception as DomainException;
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Result = new JsonResult(new
                {
                    Success = false,
                    Errors = exception?.Errors.Select(x => new
                    {
                        Code = x.Code,
                        Message = x.Message
                    })
                });
            }
        }
    }
}