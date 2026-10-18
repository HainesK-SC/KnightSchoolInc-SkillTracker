using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using System.Runtime.CompilerServices;

namespace Capstone.Identity.API.Repositories
{
    public interface ISkillRepository
    {
        public Task<IReadOnlyList<Skill>> GetAllSkillsAsync(); // admin method - all skills including archived
        public Task<IReadOnlyList<Skill>> GetAllActiveSkills(); // skill tree - all skills including archived
        public Task<Skill?> GetSkillByIdAsync(int id);
        public Task<Skill?> GetSkillByIdForUpdate(int id);
        public Task<IReadOnlyList<Skill>> GetAllSkillsInDisciplineAsync(int disciplineId);
        public void AddNewSkill(Skill skill);
        
    }
}
