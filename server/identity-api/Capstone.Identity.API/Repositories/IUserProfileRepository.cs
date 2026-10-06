using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetApplicationUserByIdAsync(Guid applicationUserId);
        void AddUserProfile(UserProfile profile);
    }
}
