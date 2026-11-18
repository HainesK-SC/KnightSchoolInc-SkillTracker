using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface IEventSkillRepository
    {
        Task<List<EventSkill>> GetEventSkillsForUpdateAsync(int eventId);
        void AddEventSkills(IEnumerable<EventSkill> eventSkills);
        void RemoveEventSkills(IEnumerable<EventSkill> eventSkills);
        Task<IReadOnlyList<Skill>> GetActiveSkillsForEventAsync(int eventId);
    }
}
