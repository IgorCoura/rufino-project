using Commom.Domain.Exceptions;

namespace Identity.API.Application.Errors
{
    public enum IdentityErrors
    {
        [ApiError("10", "O campo {TProperty} - {1}")]
        InvalidField,

        [ApiError("100", "Erro ao realizar autenticação do usuario.")]
        AuthenticationFailed,

        [ApiError("200", "O Refresh Token recebido é invalido.")]
        RefreshTokenInvalid,
    }
}
