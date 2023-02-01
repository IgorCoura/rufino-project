using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.Exceptions
{
    public static class RecoverError
    {
        public static ApiValidationError GetApiError(Enum error, params string[] msgParms)
        {
            var result = GetValues(error);
            result.Msg = String.Format(result.Msg, msgParms);
            return result;
        }

        public static string GetCode(Enum error)
        {
            var result = GetValues(error);
            return result.Code;
        }

        public static string GetMessage(Enum error, params string[] msgParms)
        {
            var result = GetValues(error);
            result.Msg = String.Format(result.Msg, msgParms);
            return result.Msg;
        }

        private static ApiValidationError GetValues(Enum error)
        {
            var enumType = error.GetType();
            var memberInfos = enumType.GetMember(error.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo!.GetCustomAttributes(typeof(ApiErrorAttribute), false);
            var erroCode = ((ApiErrorAttribute)valueAttributes[0]).ErrorCode;
            var message = ((ApiErrorAttribute)valueAttributes[0]).ErrorMsgTemplate;
            return new ApiValidationError(erroCode, message);
        }
    }
}
