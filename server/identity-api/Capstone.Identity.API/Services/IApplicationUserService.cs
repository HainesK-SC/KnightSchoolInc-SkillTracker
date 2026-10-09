using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Admin;
using Capstone.Identity.API.Dtos.Auth;
using Capstone.Identity.API.Models;
using Microsoft.AspNetCore.Identity.Data;

namespace Capstone.Identity.API.Services
{
    public interface IApplicationUserService
    {
        Task<Result<ApplicationUser>> CreateUserWithProfileAsync(
            string? email, 
            string? password,
            string firstName,
            string lastName,
            IReadOnlyCollection<string>? additionalRoles = null);
        Task<Result<CurrentUserResponseDto>> GetCurrentUserAsync(Guid userId);
        Task<Result<AdminUserDto>> CreateUserAsAdminAsync(CreateUserRequestDto request);
        Task<Result<IReadOnlyList<AdminUserDto>>> GetUsersForAdminAsync();
        Task<Result<AdminUserDto>> GetUserForAdminAsync(Guid userId);
        Task<Result<AdminUserDto>> UpdateUserAsAdminAsync(Guid userId, UpdateUserRequestDto request, Guid actingAdminId);
        Task<Result<bool>> DeleteUserAsAdminAsync(Guid userId, Guid actingAdminId);
    }
}
