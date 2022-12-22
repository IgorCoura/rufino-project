using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.FunctionIdAuthorization
{
    public class FunctionIdAuthorizationOptions
    {
        private string _jwksUri = string.Empty;

        public const string Jwt = "Jwt";
        public string? Issuer { get; set; }

        public string JwksUri { 
            get{
                return _jwksUri;
            }
            set { 
                _jwksUri= value;
                var jwks = new Uri(value);
                Issuer ??= $"{jwks.Scheme}://{jwks.Authority}";
            } 
        }



    }
}
