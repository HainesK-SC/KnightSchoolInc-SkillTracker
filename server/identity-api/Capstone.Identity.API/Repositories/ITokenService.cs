using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface ITokenService
    {
        Task<string> GenerateTokenAsync(ApplicationUser appUser);
    }
}
