using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public sealed class UserProfileRepository : IUserProfileRepository
    {
        private readonly AppDbContext _context;

        public UserProfileRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfile?> GetUserProfileByUserIdAsync(Guid applicationUserId)
        {
            var userProfile = _context.UserProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.ApplicationUserId == applicationUserId);

            return userProfile.Result;
        }

        public void AddUserProfile(UserProfile profile)
        {
            _context.UserProfiles.Add(profile);
        }

        public Task<UserProfile?> GetByApplicationUserIdForUpdateAsync(Guid applicationUserId)
        {
            return _context.UserProfiles
                .SingleOrDefaultAsync(p => p.ApplicationUserId == applicationUserId);
        }
    }
}
