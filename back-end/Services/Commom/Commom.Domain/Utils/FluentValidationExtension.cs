using Commom.Domain.Exceptions;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.Utils
{
    public static class FluentValidationExtension
    {
        public static IRuleBuilderOptions<T, TProperty> WithErrorMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Enum errorMessage, params string[] msgParms)
        {
            var error = RecoverError.GetApiError(errorMessage, msgParms);
            rule.WithMessage(error.Message);
            rule.WithErrorCode(error.Code);

            return rule;
        }
    }
}
