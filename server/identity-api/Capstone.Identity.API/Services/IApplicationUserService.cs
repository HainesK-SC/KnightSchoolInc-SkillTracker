using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Dtos.Auth;
using Capstone.Identity.API.Models;
using Microsoft.AspNetCore.Identity.Data;

namespace Capstone.Identity.API.Services
{
    public interface IApplicationUserService
    {
        //Task<Result<AuthResult>> RegisterAsync(RegisterRequestDto registerRequest);
        //Task<Result<AuthResult>> LoginAsync(LoginRequestDto request);
        Task<Result<ApplicationUser>> CreateUserWithProfileAsync(
    string? email, string? password, string firstName, string lastName);
    }
}
