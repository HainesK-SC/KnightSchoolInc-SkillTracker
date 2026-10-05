using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Dtos.Auth;
using Microsoft.AspNetCore.Identity.Data;

namespace Capstone.Identity.API.Repositories
{
    public interface IApplicationUserService
    {
        Task<Result<AuthResult>> RegisterAsync(RegisterRequestDto registerRequest);
    }
}
