using Commom.Domain.BaseEntities;
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
        public static IRuleBuilderOptions<T, TProperty> WithErrorMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Enum apiError, params Func<T, string>[] messagesProvider)
        {
            var errorCode = RecoverError.GetCode(apiError);
            var errorMessage = RecoverError.GetMessage(apiError);

            DefaultValidatorOptions.Configurable(rule).Current.SetErrorMessage((ctx, val) => {
                var msgParms = messagesProvider.Select(m => m(ctx.InstanceToValidate)).ToArray() ?? new string[] { "" };
                return String.Format(errorMessage, msgParms);
            });
            
            rule.WithErrorCode(errorCode);

            return rule;
        }
    }
}
