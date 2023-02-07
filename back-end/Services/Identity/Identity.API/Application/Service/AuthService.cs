using Commom.Domain.PasswordHasher;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Model;
using Identity.API.Application.Entities;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Commom.Domain.Exceptions;
using Identity.API.Application.Errors;
using Identity.API.Application.Options;

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
                throw new BadRequestException(IdentityErrors.AuthenticationFailed);

            var hashResult = _passwordHasherService.VerifyHashedPassword(user.Password, loginModel.Password);

            if (hashResult == PasswordVerificationResult.Failed)
            {
                throw new BadRequestException(IdentityErrors.AuthenticationFailed);
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
                CleanRefreshTokens();
                return tokens;
            }

            throw new BadRequestException(IdentityErrors.AuthenticationFailed);
        }

        public async Task<TokenModel> RefreshToken(string refreshToken)
        {
            var claims = _tokenService.ValidateRefreshToken(refreshToken);

            var credentials = GetClaims(claims);

            var newRefreshToken = RecreateUserRefreshToken(credentials.Id, credentials.UserId);
            var user = GetUser(credentials.UserId);            

            var tokens = await _tokenService.GenerateTokens(await user, await newRefreshToken);
            await _userRefreshTokenRepository.UnitOfWork.SaveChangesAsync();

            return tokens;     
        }

        public async Task SingOutLocal(string refreshToken)
        {
            var claims = _tokenService.ValidateRefreshToken(refreshToken);

            var credentials = GetClaims(claims);

            await _userRefreshTokenRepository.DeleteAsync(new (){Id = credentials.Id});
            await _userRefreshTokenRepository.UnitOfWork.SaveChangesAsync();
        }

        public async Task SingOutAll(string refreshToken)
        {
            var claims = _tokenService.ValidateRefreshToken(refreshToken);
            var credentials = GetClaims(claims);
            var tokens = await _userRefreshTokenRepository.GetDataAsync(x => x.UserId == credentials.UserId);

            if(!tokens.Any(x => x.Id == credentials.Id))
                throw new BadRequestException(IdentityErrors.RefreshTokenInvalid);

            await _userRefreshTokenRepository.DeleteRangeAsync(tokens);
            await _userRefreshTokenRepository.UnitOfWork.SaveChangesAsync();
        }

        private async Task<UserRefreshToken> CreateUserRefreshToken(Guid userId)
        {
            var userRefreshToken = new UserRefreshToken()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExpireDate = DateTime.UtcNow.AddMinutes(_authenticationOptions.ExpireRefreshToken)
            };

            var result = await _userRefreshTokenRepository.RegisterAsync(userRefreshToken);

            return result;
        }

        private async Task<UserRefreshToken> RecreateUserRefreshToken(Guid tokenId, Guid userId)
        {
            var currentRefreshToken =
                await _userRefreshTokenRepository.FirstAsync(x => x.Id == tokenId && x.UserId == userId)
                ?? throw new BadRequestException(IdentityErrors.RefreshTokenInvalid);

            await _userRefreshTokenRepository.DeleteAsync(currentRefreshToken);

            return await CreateUserRefreshToken(userId);
        }

        private async Task<User> GetUser(Guid userId)
        {
            var user = await _userRepository.FirstAsync(x => x.Id == userId)
                ?? throw new BadRequestException(IdentityErrors.RefreshTokenInvalid);
            return user;
        }

        private UserRefreshToken GetClaims(ClaimsPrincipal claims)
        {
            try
            {
                string? tokenId = claims.FindFirst(c => c.Type == ClaimTypes.Sid)?.Value;
                string? userId = claims.FindFirst(c => c.Type == ClaimTypes.Actor)?.Value;
                return new UserRefreshToken()
                {
                    Id = Guid.Parse(tokenId!),
                    UserId = Guid.Parse(userId!),
                };
            }
            catch
            {
                throw new BadRequestException(IdentityErrors.RefreshTokenInvalid);
            }
        }

        private async Task CleanRefreshTokens()
        {
            var tokens = await _userRefreshTokenRepository.GetDataAsync(x => x.ExpireDate < DateTime.Now) 
                ?? new List<UserRefreshToken>();
            await _userRefreshTokenRepository.DeleteRangeAsync(tokens);
            await _userRefreshTokenRepository.UnitOfWork.SaveChangesAsync();
        }
    }
}
