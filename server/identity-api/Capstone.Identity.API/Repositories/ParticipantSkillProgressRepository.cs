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

        public async Task<bool> AnyProgressForSkillAsync(int skillId)
        {
            var isProgress = await _appDbContext.ParticipantSkillProgresses.AnyAsync(p => p.SkillId == skillId);

            return isProgress;
        }

        public async Task<bool> AnyProgressInDisciplineAsync(int disciplineId)
        {
            var isProgress = await _appDbContext.ParticipantSkillProgresses
                .AnyAsync(p => _appDbContext.Skills
                    .Any(s => s.SkillId == p.SkillId && s.DisciplineId == disciplineId));

            return isProgress;
        }
    }
}