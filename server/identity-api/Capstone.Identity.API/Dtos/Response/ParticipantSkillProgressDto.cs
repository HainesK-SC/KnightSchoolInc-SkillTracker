using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Dtos.Response
{
    public class ParticipantSkillProgressDto
    {
        public Guid UserId { get; set; }

        public int SkillId { get; set; }

        public SkillStatus Status { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public Guid? UpdatedById { get; set; }
    }
}
