using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Services
{
    public interface ITokenService
    {
        Task<AccessToken> GenerateTokenAsync(ApplicationUser appUser);
    }
}
