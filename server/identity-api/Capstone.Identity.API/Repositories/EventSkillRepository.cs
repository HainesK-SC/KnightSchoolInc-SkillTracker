using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public class EventSkillRepository : IEventSkillRepository
    {
        private readonly AppDbContext _context;

        public EventSkillRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<EventSkill>> GetEventSkillsForUpdateAsync(int eventId)
        {
            var eventSkills = await _context.EventSkills.ToListAsync();

            return eventSkills;
        }
        public void AddEventSkills(IEnumerable<EventSkill> eventSkills)
        {
            _context.AddRange(eventSkills);
        }
        public void RemoveEventSkills(IEnumerable<EventSkill> eventSkills)
        {
            _context.RemoveRange(eventSkills);
        }

        public async Task<IReadOnlyList<Skill>> GetActiveSkillsForEventAsync(int eventId)
        {
            var skills = 
                from es in _context.EventSkills
                join skill in _context.Skills
                    on es.SkillId equals skill.SkillId
                where es.EventId == eventId
                select skill;

            return await skills.AsNoTracking().ToListAsync();
        }
    }
}
