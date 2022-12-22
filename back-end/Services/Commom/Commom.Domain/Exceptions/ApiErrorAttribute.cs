using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.Exceptions
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ApiErrorAttribute : Attribute
    {
        public ApiErrorAttribute(string errorCode, string errorMsgTemplate)
        {
            ErrorCode = errorCode;
            ErrorMsgTemplate = errorMsgTemplate;
        }

        public string ErrorCode { get; }
        public string ErrorMsgTemplate { get; }

    }
}
