using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Dtos.Auth;
using Capstone.Identity.API.Models;
using Microsoft.AspNetCore.Identity;

namespace Capstone.Identity.API.Services
{
    public class AuthService : IAuthService
    {
        private const string InvalidCredentials = "Invalid email or password.";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IApplicationUserService _applicationUserService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IApplicationUserService applicationUserService,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationUserService = applicationUserService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<AuthResult>> RegisterAsync(RegisterRequestDto request)
        {
            var createResult = await _applicationUserService.CreateUserWithProfileAsync(
                request.Email, request.Password, request.FirstName, request.LastName);

            if (!createResult.Succeeded)
            {
                return Result<AuthResult>.Failure(createResult.Error!, createResult.ErrorType);
            }

            var user = createResult.Data!;
            _logger.LogInformation("User {UserId} registered.", user.Id);

            return await IssueAuthResultAsync(user);
        }

        public async Task<Result<AuthResult>> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                _logger.LogWarning("Login failed: no account matches the provided email.");
                return Result<AuthResult>.Failure(InvalidCredentials, ResultErrorType.Unauthorized);
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("Login blocked: user {UserId} is locked out.", user.Id);
                return Result<AuthResult>.Failure(
                    "This account is temporarily locked after too many failed attempts. Please try again in a few minutes.",
                    ResultErrorType.Unauthorized);
            }

            if (signInResult.IsNotAllowed)
            {
                _logger.LogWarning("Login blocked: user {UserId} is not allowed to sign in.", user.Id);
                return Result<AuthResult>.Failure(InvalidCredentials, ResultErrorType.Unauthorized);
            }

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Login failed: incorrect password for user {UserId}.", user.Id);
                return Result<AuthResult>.Failure(InvalidCredentials, ResultErrorType.Unauthorized);
            }

            _logger.LogInformation("User {UserId} logged in.", user.Id);

            return await IssueAuthResultAsync(user);
        }

        private async Task<Result<AuthResult>> IssueAuthResultAsync(ApplicationUser user)
        {
            var currentUser = await _applicationUserService.GetCurrentUserAsync(user.Id);
            if (!currentUser.Succeeded)
            {
                return Result<AuthResult>.Failure(currentUser.Error!, currentUser.ErrorType);
            }

            var token = await _tokenService.GenerateTokenAsync(user);

            return Result<AuthResult>.Success(new AuthResult(currentUser.Data!, token));
        }

        public async Task<Result<CurrentUserResponseDto>> GetCurrentUserAsync(Guid userId)
        {
            var result = await _applicationUserService.GetCurrentUserAsync(userId);

            if (!result.Succeeded && result.ErrorType == ResultErrorType.NotFound)
            {
                _logger.LogWarning(
                    "Valid token presented for user {UserId}, but the user no longer exists.", userId);

                return Result<CurrentUserResponseDto>.Failure(
                    "Your session is no longer valid. Please log in again.", ResultErrorType.Unauthorized);
            }

            return result;
        }
    }
}
