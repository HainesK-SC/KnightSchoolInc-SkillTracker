using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Admin;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDisplayNameGenerator _displayNameGenerator;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserQueryRepository _userQueryRepository;

        public ApplicationUserService(
            ILogger<ApplicationUserService> logger,
            UserManager<ApplicationUser> userManager,
            IDisplayNameGenerator displayNameGenerator,
            IUserProfileRepository userProfileRepostory,
            IUnitOfWork unitOfWork,
            IUserQueryRepository userQueryRepository)
        {
            _logger = logger;
            _userManager = userManager;
            _displayNameGenerator = displayNameGenerator;
            _userProfileRepository = userProfileRepostory; 
            _unitOfWork = unitOfWork;
            _userQueryRepository = userQueryRepository;
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
            string? email,
            string? password,
            string firstName,
            string lastName,
            IReadOnlyCollection<string>? additionalRoles = null)
        {
            if (password is not null && email is null)
            {
                return Result<ApplicationUser>.Failure(
                    "An email is required when a password is provided.", ResultErrorType.Validation);
            }

            //if (password is null && email is not null)
            //{
            //    return Result<ApplicationUser>.Failure(
            //        "A password is required to login.", ResultErrorType.Validation);
            //}

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

            // This block checks the roles being sent in a request to create a new user
            // It contains a validation that ensures no unknown roles are trying to be passed
            // If all roles are valid, it simply adds the roles to the new user
            var requestedRoles = additionalRoles ?? [];

            var unknownRoles = requestedRoles
                .Where(role => !Roles.GetAllRoles().Contains(role))
                .ToList();

            if (unknownRoles.Count > 0)
            {
                return Result<ApplicationUser>.Failure(
                    $"Unknown role(s): {string.Join(", ", unknownRoles)}.",
                    ResultErrorType.Validation);
            }

            var rolesToAssign = requestedRoles
                .Append(Roles.RegularUser)
                .Distinct()
                .ToList();

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

            //var roleResult = await _userManager.AddToRoleAsync(user, Roles.RegularUser);

            //if (!roleResult.Succeeded)
            //{
            //    _logger.LogWarning(
            //        "Default role assignment failed: {ErrorCodes}",
            //        roleResult.Errors.Select(e => e.Code)
            //        );

            //    var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
            //    return Result<ApplicationUser>.Failure(errors, ResultErrorType.Validation);
            //}

            var roleResult = await _userManager.AddToRolesAsync(user, rolesToAssign);

            if (!roleResult.Succeeded)
            {
                _logger.LogError(
                    "Role assignment failed for new user {UserId}: {ErrorCodes}",
                    user.Id,
                    roleResult.Errors.Select(e => e.Code));

                return Result<ApplicationUser>.Failure(
                    "An unexpected error occurred.", ResultErrorType.Unexpected);
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

        // Admin related ApplicationUser methods
        public async Task<Result<AdminUserDto>> CreateUserAsAdminAsync(CreateUserRequestDto request)
        {
            var createResult = await CreateUserWithProfileAsync(
                request.Email,
                null,
                request.FirstName,
                request.LastName,
                request.Roles);

            if (!createResult.Succeeded)
            {
                return Result<AdminUserDto>.Failure(createResult.Error!, createResult.ErrorType);
            }

            return await GetUserForAdminAsync(createResult.Data!.Id);
        }

        public async Task<Result<IReadOnlyList<AdminUserDto>>> GetUsersForAdminAsync()
        {
            var rows = await _userQueryRepository.GetAllWithProfilesAsync();

            var users = rows
                .Select(row => row.ToAdminUserDto())
                .ToList();

            return Result<IReadOnlyList<AdminUserDto>>.Success(users);
        }

        public async Task<Result<AdminUserDto>> GetUserForAdminAsync(Guid userId)
        {
            var row = await _userQueryRepository.GetByIdWithProfileAsync(userId);

            if (row is null)
            {
                return Result<AdminUserDto>.Failure("User not found.", ResultErrorType.NotFound);
            }

            return Result<AdminUserDto>.Success(row.ToAdminUserDto());
        }
    }
}
