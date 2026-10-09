using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Models.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public sealed class UserQueryRepository : IUserQueryRepository
    {
        private readonly AppDbContext _db;

        public UserQueryRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<UserWithProfile>> GetAllWithProfilesAsync()
        {
            var results = await WithProfilesAndRoles(_db.Users.AsNoTracking())
                .ToListAsync();

            return results
                .OrderBy(r => r.User.LastName)
                .ThenBy(r => r.User.FirstName)
                .ToList();
        }

        public async Task<UserWithProfile?> GetByIdWithProfileAsync(Guid userId)
        {
            var users = _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId);

            return await WithProfilesAndRoles(users).SingleOrDefaultAsync();
        }

        private IQueryable<UserWithProfile> WithProfilesAndRoles(IQueryable<ApplicationUser> users)
        {
            return
                from user in users
                join profile in _db.UserProfiles.AsNoTracking()
                    on user.Id equals profile.ApplicationUserId
                select new UserWithProfile(
                    user,
                    profile,
                    (from userRole in _db.UserRoles
                     join role in _db.Roles on userRole.RoleId equals role.Id
                     where userRole.UserId == user.Id
                     select role.Name!).ToList());
        }
    }
}
