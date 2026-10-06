using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Dtos.Auth;

namespace Capstone.Identity.API.Services
{
    public interface IAuthService
    {
        Task<Result<AuthResult>> RegisterAsync(RegisterRequestDto request);
        Task<Result<AuthResult>> LoginAsync(LoginRequestDto request);
        Task<Result<CurrentUserResponseDto>> GetCurrentUserAsync(Guid userId);
    }
}
