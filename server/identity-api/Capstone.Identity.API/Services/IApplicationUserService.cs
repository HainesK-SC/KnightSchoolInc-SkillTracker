using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Auth;
using Capstone.Identity.API.Models;
using Microsoft.AspNetCore.Identity.Data;

namespace Capstone.Identity.API.Services
{
    public interface IApplicationUserService
    {
        Task<Result<ApplicationUser>> CreateUserWithProfileAsync(
    string? email, string? password, string firstName, string lastName);
        Task<Result<CurrentUserResponseDto>> GetCurrentUserAsync(Guid userId);
    }
}
