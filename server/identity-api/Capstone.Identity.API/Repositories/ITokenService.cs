using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface ITokenService
    {
        Task<AccessToken> GenerateTokenAsync(ApplicationUser appUser);
    }
}
