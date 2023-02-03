using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.Exceptions
{
    public class ApiValidationError
    {
        public ApiValidationError(string code, string msg)
        {
            Code = code;
            Message = msg;
        }

        public string Code { get; set; }
        public string Message { get; set; }
    }
}
