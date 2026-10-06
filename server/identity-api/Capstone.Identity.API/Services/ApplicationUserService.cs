using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Auth;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Net.NetworkInformation;

namespace Capstone.Identity.API.Services
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _appDbContext;
        private readonly IDisplayNameGenerator _displayNameGenerator;
        private readonly ITokenService _tokenService;

        public ApplicationUserService(
            UserManager<ApplicationUser> userManager,
            AppDbContext appDbContext,
            IDisplayNameGenerator displayNameGenerator,
            ITokenService tokenService)
        {
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
            ApplicationUser user, UserProfile profile, IList<string> roles) => new()
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = profile.DisplayName,
                AvatarImagePath = profile.AvatarImagePath,
                Roles = roles.ToList()
            };
    }
}
