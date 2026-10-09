using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetUserProfileByUserIdAsync(Guid applicationUserId);
        void AddUserProfile(UserProfile profile);
        Task<UserProfile?> GetByApplicationUserIdForUpdateAsync(Guid applicationUserId);
    }
}
