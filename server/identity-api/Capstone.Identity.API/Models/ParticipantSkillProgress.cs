using Capstone.Identity.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Models
{
    [Index(nameof(UserProfileId), nameof(SkillId), IsUnique = true)]
    public class ParticipantSkillProgress
    {
        public int ParticipantSkillProgressId { get; set; } = default;
        public int UserProfileId { get; set; } = default; // UserProfile FK
        public int SkillId { get; set; } = default; // Skill FK
        public SkillStatus Status{ get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public Guid UpdatedById { get; set; } = default;

    }
}
