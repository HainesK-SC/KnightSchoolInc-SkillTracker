using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Auth;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace Capstone.Identity.API.Services
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly ILogger<ApplicationUserService> _logger;
        private readonly AppDbContext _appDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDisplayNameGenerator _displayNameGenerator;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ApplicationUserService(
            UserManager<ApplicationUser> userManager,
            IDisplayNameGenerator displayNameGenerator,
            IUserProfileRepository userProfileRepostory,
            IUnitOfWork unitOfWork,
            ILogger<ApplicationUserService> logger)
        {
            _userManager = userManager;
            _displayNameGenerator = displayNameGenerator;
            _userProfileRepository = userProfileRepostory; 
            _unitOfWork = unitOfWork;
            _logger = logger;
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

        public async Task<Result<ApplicationUser>> CreateUserWithProfileAsync(
            string? email, string? password, string firstName, string lastName)
        {
            if (password is not null && email is null)
            {
                return Result<ApplicationUser>.Failure(
                    "An email is required when a password is provided.", ResultErrorType.Validation);
            }

            if (password is null && email is not null)
            {
                return Result<ApplicationUser>.Failure(
                    "A password is required to login.", ResultErrorType.Validation);
            }

            // Reject duplicate emails
            if (email is not null && await _userManager.FindByEmailAsync(email) is not null)
            {
                _logger.LogWarning("User creation rejected: email is already in use.");
                return Result<ApplicationUser>.Failure(
                    "An account with this email already exists.", ResultErrorType.Conflict);
            }

            var user = new ApplicationUser
            {
                UserName = email ?? $"placeholder-{Guid.NewGuid():N}",
                Email = email,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim()
            };

            // User and profile succeed or fail together
            // Keeping this atomic has to happen to prevent orphaned UserProfiles
            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            var createResult = password is null
                ? await _userManager.CreateAsync(user)
                : await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                _logger.LogWarning(
                    "User creation failed with Identity errors: {ErrorCodes}",
                    createResult.Errors.Select(e => e.Code));

                var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return Result<ApplicationUser>.Failure(errors, ResultErrorType.Validation);
            }

            // Create the profile object, then save and commit
            var salutation = _displayNameGenerator.DefaultSalutation;
            var modifier = _displayNameGenerator.PickRandomModifier();

            _userProfileRepository.AddUserProfile(new UserProfile
            {
                ApplicationUserId = user.Id,
                DisplayNameSalutation = salutation,
                DisplayNameModifier = modifier,
                DisplayName = _displayNameGenerator.GenerateDisplayName(salutation, user.FirstName, modifier).ToString()
            });

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Created user {UserId} (placeholder: {IsPlaceholder}).", user.Id, password is null);

            return Result<ApplicationUser>.Success(user);
        }

        public async Task<Result<CurrentUserResponseDto>> GetCurrentUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                _logger.LogWarning("Current user lookup failed: user {UserId} not found.", userId);
                return Result<CurrentUserResponseDto>.Failure("User not found.", ResultErrorType.NotFound);
            }

            var profile = await _userProfileRepository.GetUserProfileByUserIdAsync(user.Id);
            if (profile is null)
            {
                _logger.LogError("User {UserId} has no UserProfile.", user.Id);
                return Result<CurrentUserResponseDto>.Failure(
                    "An unexpected error occurred.", ResultErrorType.Unexpected);
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Result<CurrentUserResponseDto>.Success(ToCurrentUserResponseDto(user, profile, roles));
        }
    }
}
