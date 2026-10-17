using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public class ParticipantSkillProgressRepository : IParticipantSkillProgressRepository
    {
        private readonly AppDbContext _appDbContext;

        public ParticipantSkillProgressRepository(AppDbContext context)
        {
            _appDbContext = context;
        }

        public async void AddNewProgressRow(ParticipantSkillProgress progress)
        {
            _appDbContext.ParticipantSkillProgresses.Add(progress);
        }

        public async Task<ParticipantSkillProgress?> GetForUpdateAsync(int userProfileId, int skillId)
        {
            var progressRow = await _appDbContext.ParticipantSkillProgresses
                .FirstOrDefaultAsync(p => p.UserProfileId == userProfileId && p.SkillId == skillId);

            return progressRow;
        }

        public async Task<IReadOnlyList<ParticipantSkillProgress>> GetForUserAsync(int userProfileId)
        {
            var allProgressForUser = await _appDbContext.ParticipantSkillProgresses
                .AsNoTracking()
                .Where(p => p.UserProfileId == userProfileId)
                .ToListAsync();

            return allProgressForUser;
        }

        public void RemoveProgressRow(ParticipantSkillProgress progress)
        {
            _appDbContext.ParticipantSkillProgresses.Remove(progress);
        }
    }
}