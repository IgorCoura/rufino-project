using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Exceptions
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ApiErrorAttribute : Attribute
    {
        public ApiErrorAttribute(int errorCode, string errorMsgTemplate)
        {
            ErrorCode = errorCode;
            ErrorMsgTemplate = errorMsgTemplate;
        }

        public int ErrorCode { get; }
        public string ErrorMsgTemplate { get; }

    }
}
