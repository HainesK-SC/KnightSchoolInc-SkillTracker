using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Dtos.Response
{
    public class SkillTreeSkillDto
    {
        public int SkillId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? Benefits { get; set; }
        public SkillStatus Status { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
