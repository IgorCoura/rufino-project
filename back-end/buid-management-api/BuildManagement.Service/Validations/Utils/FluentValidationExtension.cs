using BuildManagement.Domain.Exceptions;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Validations.Utils
{
    public static class FluentValidationExtension
    {
        public static IRuleBuilderOptions<T, TProperty> WithErrorMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Enum errorMessage, params Func<T, string>[] messagesProvider)
        {
            var enumType = typeof(ErrorsMessages);
            var memberInfos = enumType.GetMember(errorMessage.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo!.GetCustomAttributes(typeof(ApiErrorAttribute), false);
            var erroCode = ((ApiErrorAttribute)valueAttributes[0]).ErrorCode;
            var message = ((ApiErrorAttribute)valueAttributes[0]).ErrorMsgTemplate;

            DefaultValidatorOptions.Configurable(rule).Current.SetErrorMessage((ctx, val) => {
                message = ctx.MessageFormatter.BuildMessage(message);
                var msgParms = messagesProvider.Select(m => m(ctx.InstanceToValidate)).ToArray() ?? new string[] {""};
                return String.Format(message, msgParms);
            });

            rule.WithErrorCode(erroCode);

            return rule;
        }
    }
}
