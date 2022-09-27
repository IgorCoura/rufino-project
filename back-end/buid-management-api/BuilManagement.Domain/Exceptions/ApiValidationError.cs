using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Exceptions
{
    public class ApiValidationError
    {
        public ApiValidationError(int code, string msg) { 
            Code = code;
            Msg = msg;
        }

        public int Code { get; set; }
        public string Msg { get; set; }
    }
}
