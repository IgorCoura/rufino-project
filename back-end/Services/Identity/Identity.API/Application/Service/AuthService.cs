using Commom.API.Authentication;
using Commom.Domain.PasswordHasher;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Model;
using Identity.API.Application.Entities;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Identity.API.Application.Service
{
    public class AuthService : IAuthService
    {
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly ITokenService _tokenService;
        private readonly AuthenticationOptions _authenticationOptions;
        private readonly IUserRepository _userRepository;
        private readonly IUserRefreshTokenRepository _userRefreshTokenRepository;

        public AuthService(IPasswordHasherService passwordHasherService, ITokenService tokenService, IOptions<AuthenticationOptions> options, IUserRepository userRepository, IUserRefreshTokenRepository userRefreshTokenRepository)
        {
            _passwordHasherService = passwordHasherService;
            _tokenService = tokenService;
            _authenticationOptions = options.Value;
            _userRepository = userRepository;
            _userRefreshTokenRepository = userRefreshTokenRepository;
        }

        public async Task<TokenModel> SingIn(LoginModel loginModel)
        {
            
            var user = await _userRepository.FirstAsyncAsTracking(u => u.Username == loginModel.Username) ??
                throw new Exception(); //TODO: Mudar exception

            var hashResult = _passwordHasherService.VerifyHashedPassword(user.Password, loginModel.Password);

            if (hashResult == PasswordVerificationResult.Failed)
            {
                throw new Exception(); //TODO: Mudar exception
            }

            if (hashResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                var newPasswordHash = _passwordHasherService.HashPassword(loginModel.Password);
                user.Password = newPasswordHash;
                hashResult = PasswordVerificationResult.Success;
            }

            if (hashResult == PasswordVerificationResult.Success)
            {
                var userRefreshToken = await CreateUserRefreshToken(user.Id);
                var tokens = await _tokenService.GenerateTokens(user, userRefreshToken);
                await _userRepository.UnitOfWork.SaveChangesAsync();
                return tokens;
            }

            throw new Exception(); //TODO: Mudar exception
        }

        public async Task<TokenModel> RefreshToken(string refreshToken)
        {
            var claims = _tokenService.ValidateRefreshToken(refreshToken);

            string tokenId = claims.FindFirst(c => c.Type == ClaimTypes.Sid)?.Value ?? throw new Exception();
            string userId = claims.FindFirst(c => c.Type == ClaimTypes.Actor)?.Value ?? throw new Exception();

            var currentRefreshTokne = await _userRefreshTokenRepository.FirstAsync(x => x.Id.Equals(tokenId) && x.UserId.Equals(userId)) ?? throw new Exception(); //TODO: Mudar exception;
            await _userRefreshTokenRepository.DeleteAsync(currentRefreshTokne);

            var user = await _userRepository.FirstAsync(x => x.Id.Equals(userId)) ?? throw new Exception(); //TODO: Mudar exception;

            var newRefreshToken = await CreateUserRefreshToken(user.Id);

            return await _tokenService.GenerateTokens(user, newRefreshToken);       
        }

        public void SingOutLocal(string refreshToken)
        {
            var claims = _tokenService.ValidateRefreshToken(refreshToken);

        }

        public void SingOutAll()
        {

        }

        public async Task<UserRefreshToken> CreateUserRefreshToken(Guid userId)
        {
            var userRefreshToken = new UserRefreshToken()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExpireDate = DateTime.UtcNow.AddMinutes(_authenticationOptions.ExpireRefreshToken)
            };

            var result = await _userRefreshTokenRepository.RegisterAsync(userRefreshToken) ?? throw new Exception(); //TODO: Mudar exception;

            return result;
        }
    }
}
