using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Models
{
    [Index(nameof(EventId), nameof(SkillId), IsUnique = true)]
    public class EventSkill
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int SkillId { get; set; }
    }
}
