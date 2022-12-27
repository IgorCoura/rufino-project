using Commom.Domain.Exceptions;

namespace Identity.API.Application.Errors
{
    public enum IdentityErrors
    {

        [ApiError("1010", "Erro ao realizar autenticação do usuario.")]
        AuthenticationFailed,

        [ApiError("1020", "O Refresh Token recebido é invalido.")]
        RefreshTokenInvalid,
    }
}
