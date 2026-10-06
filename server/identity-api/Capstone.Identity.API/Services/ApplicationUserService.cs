using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Auth;
using Capstone.Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace Capstone.Identity.API.Services
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ApplicationUserService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _appDbContext;
        private readonly IDisplayNameGenerator _displayNameGenerator;
        private readonly ITokenService _tokenService;

        public ApplicationUserService(
            SignInManager<ApplicationUser> signInManager,
            ILogger<ApplicationUserService> logger,
            UserManager<ApplicationUser> userManager,
            AppDbContext appDbContext,
            IDisplayNameGenerator displayNameGenerator,
            ITokenService tokenService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _appDbContext = appDbContext;
            _displayNameGenerator = displayNameGenerator;
            _tokenService = tokenService;
        }

        public async Task<Result<AuthResult>> RegisterAsync(RegisterRequestDto registerRequest)
        {
            var existing = await _userManager.FindByEmailAsync(registerRequest.Email);
            if (existing != null)
            {
                _logger.LogWarning("Error registering account: Email:{UserId} already exists", registerRequest.Email);
                return Result<AuthResult>
                    .Failure("An account with this email already exists.", ResultErrorType.Conflict);
            }

            var user = new ApplicationUser
            {
                
                UserName = registerRequest.Email,
                Email = registerRequest.Email,
                FirstName = registerRequest.FirstName.Trim(),
                LastName = registerRequest.LastName.Trim()
            };

            // this is wrapped in a transaction to ensure it's an atomic operation
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            var newUserResult = await _userManager.CreateAsync(user, registerRequest.Password);
            if (!newUserResult.Succeeded)
            {
                var errors = string.Join(" ", newUserResult.Errors.Select(e => e.Description));
                return Result<AuthResult>.Failure(errors, ResultErrorType.Validation);
            }

            var salutation = _displayNameGenerator.DefaultSalutation;
            var modifier = _displayNameGenerator.PickRandomModifier();

            var profile = new UserProfile
            {
                ApplicationUserId = user.Id,
                DisplayNameSalutation = salutation,
                DisplayNameModifier = modifier,
                DisplayName = _displayNameGenerator.GenerateDisplayName(salutation, user.FirstName, modifier).Result
            };

            _appDbContext.Add(profile);
            await _appDbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            var token = await _tokenService.GenerateTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Result<AuthResult>.Success(
                new AuthResult(ToCurrentUserResponseDto(user, profile, roles), token));
        }

        private static CurrentUserResponseDto ToCurrentUserResponseDto(
            ApplicationUser user, 
            UserProfile profile, 
            IList<string> roles) => new()
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = profile.DisplayName,
            AvatarImagePath = profile.AvatarImagePath,
            Roles = roles.ToList()
        };

        public async Task<Result<AuthResult>> LoginAsync(LoginRequestDto request)
        {
            const string invalidCredentials = "Invalid email or password.";

            // 1. Find the account
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                _logger.LogWarning("Login failed: no account matches the provided email.");
                return Result<AuthResult>.Failure(invalidCredentials, ResultErrorType.Unauthorized);
            }

            // 2. Check the password (this also enforces lockout)
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
                return Result<AuthResult>.Failure(invalidCredentials, ResultErrorType.Unauthorized);
            }

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Login failed: incorrect password for user {UserId}.", user.Id);
                return Result<AuthResult>.Failure(invalidCredentials, ResultErrorType.Unauthorized);
            }

            // 3. Load the profile
            var profile = await _appDbContext.UserProfiles
                .AsNoTracking<UserProfile>()
                .SingleOrDefaultAsync(p => p.ApplicationUserId == user.Id);

            if (profile is null)
            {
                _logger.LogError("Login failed: user {UserId} has no UserProfile.", user.Id);
                return Result<AuthResult>.Failure("An unexpected error occurred.", ResultErrorType.Unexpected);
            }

            // 4. Issue the token and build the response
            var token = await _tokenService.GenerateTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("User {UserId} logged in.", user.Id);

            return Result<AuthResult>.Success(
                new AuthResult(ToCurrentUserResponseDto(user, profile, roles), token));
        }
    }
}
