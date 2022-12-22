using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.API.Authentication
{
    public class AuthenticationOptions
    {
        public const string Jwt = "Jwt";
        public string Issuer { get; set; } = string.Empty;
        public string JwksUri { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireToken { get; set; }
        public int ExpireRefreshToken { get; set; }
        public string KeyRefreshToken { get; set; } = string.Empty;
    }
}



