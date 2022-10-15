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
        public static IRuleBuilderOptions<T, TProperty> WithErrorMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Enum errorMessage, params Func<T, TProperty, string>[] msgParms)
        {
            var enumType = typeof(ErrorsMessages);
            var memberInfos = enumType.GetMember(errorMessage.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo!.GetCustomAttributes(typeof(ApiErrorAttribute), false);
            var erroCode = ((ApiErrorAttribute)valueAttributes[0]).ErrorCode;
            var message = ((ApiErrorAttribute)valueAttributes[0]).ErrorMsgTemplate;

            
            //TODO: Colocar variavel no erro.

            //if(msgParms.Length > 0)
            //    message = String.Format(message, msgParms);

            rule.WithMessage(message);
            rule.WithErrorCode(erroCode);

            return rule;
        }
    }
}
