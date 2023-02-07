namespace Identity.API.Application.Options
{
    public class AuthenticationOptions
    {
        public const string Section = "Jwt";
        public string Issuer { get; set; } = string.Empty;
        public string JwksUri { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireToken { get; set; }
        public int ExpireRefreshToken { get; set; }
        public string KeyRefreshToken { get; set; } = string.Empty;
    }
}



