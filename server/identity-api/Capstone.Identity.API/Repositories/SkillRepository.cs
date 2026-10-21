using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public class SkillRepository :ISkillRepository
    {
        private readonly AppDbContext _appDbContext;

        public SkillRepository(AppDbContext context)
        {
            _appDbContext = context;
        }

        public void AddNewSkill(Skill skill) 
        {
           _appDbContext.Skills.Add(skill);
        }

        // Excludes skill with the IsArchived == true
        // meaning the skill is currently archived and not being taught
        public async Task<IReadOnlyList<Skill>> GetAllActiveSkills()
        {
            var activeSkills = await _appDbContext.Skills
                .AsNoTracking()
                .Where(s => s.IsArchived == false)
                .OrderBy(s => s.Name)
                .ToListAsync<Skill>();

            return activeSkills;
        }

        // Includes all skills - even archived skills
        public async Task<IReadOnlyList<Skill>> GetAllSkillsAsync()
        {
            var allSkills = await _appDbContext.Skills
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();

            return allSkills;
        }

        public async Task<IReadOnlyList<Skill>> GetAllSkillsInDisciplineAsync(int disciplineId)
        {
            var skillsInDiscipline = await _appDbContext.Skills
                .AsNoTracking()
                .Where(s => s.DisciplineId == disciplineId)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return skillsInDiscipline;
        }

        public async Task<Skill?> GetSkillByIdAsync(int id)
        {
            var skill = await _appDbContext.Skills
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SkillId == id);

            return skill;
        }

        public async Task<Skill?> GetSkillByIdForUpdate(int id)
        {
            var skillForUpdate = await _appDbContext.Skills
                .FirstOrDefaultAsync(s => s.SkillId == id);

            return skillForUpdate;
        }

        public async Task<bool> ExistsByNameInDisciplineAsync(int disciplineId, string name, int? excludingId = null)
        {
            var query = _appDbContext.Skills
                .Where(s => s.DisciplineId == disciplineId && s.Name == name);

            if (excludingId is not null)
            {
                query = query.Where(s => s.SkillId != excludingId.Value);
            }

            return await query.AnyAsync();
        }

        public void RemoveSkill(Skill skill)
        {
            _appDbContext.Skills.Remove(skill);
        }
        public void RemoveSkills(IEnumerable<Skill> skills)
        {
            _appDbContext.Skills.RemoveRange(skills);
        }
    }
}