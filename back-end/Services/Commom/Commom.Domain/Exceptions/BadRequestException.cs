using Commom.Domain.Errors;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Commom.Domain.Exceptions
{
    public class BadRequestException : Exception
    {
        public List<ApiValidationError> Errors { get; set; } = new List<ApiValidationError>();

        public BadRequestException()
        {
        }
        public BadRequestException(string erroCode, string errorMessage)
        {
            Errors.Add(new ApiValidationError(erroCode, errorMessage));
        }

        public BadRequestException(IEnumerable<ValidationFailure> errorMessage)
        {
            Errors.AddRange(errorMessage.Select(x => new ApiValidationError(x.ErrorCode, x.ErrorMessage)));
        }

        public BadRequestException(Enum error, params string[] msgParms)
        {
            var apiError = RecoverError.GetApiError(error, msgParms);
            Errors.Add(apiError);
        }

        public void AddError(Enum error, params string[] msgParms)
        {
            var apiError = RecoverError.GetApiError(error, msgParms);
            Errors.Add(apiError);
        }

        public bool HasErrors()
        {
            return Errors.Count > 0;
        }
    }
}
