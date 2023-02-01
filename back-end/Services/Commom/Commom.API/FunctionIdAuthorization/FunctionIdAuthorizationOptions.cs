using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.FunctionIdAuthorization
{
    public class FunctionIdAuthorizationOptions
    {
        public const string Section = "Authentication";
        public string Schema { get; set; } = "Bearer";
    }
}
