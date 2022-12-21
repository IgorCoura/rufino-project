using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.FunctionIdAuthorization
{
    public class FunctionIdAuthorizationOptions
    {
        public const string Jwt = "Jwt";
        public string Issuer { get; set; }

        public string JwksUri { get; set; }

        public FunctionIdAuthorizationOptions(string jwksUri, string? issuer = null)
        {
            JwksUri = jwksUri;
            Uri uri = new(jwksUri);
            Issuer = issuer ?? (uri.Scheme + "://" + uri.Authority);
        }
    }
}
