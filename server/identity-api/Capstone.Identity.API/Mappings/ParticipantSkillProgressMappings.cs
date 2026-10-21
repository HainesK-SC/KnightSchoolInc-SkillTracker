using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Mappings
{
    public static class ParticipantSkillProgressMappings
    {
        public static ParticipantSkillProgressDto ToParticipantSkillProgressDto(
        this ParticipantSkillProgress progress,
        Guid userId)
        {
            return new ParticipantSkillProgressDto
            {
                UserId = userId,
                SkillId = progress.SkillId,
                Status = progress.Status,
                UpdatedAtUtc = progress.UpdatedAtUtc,
                UpdatedById = progress.UpdatedById
            };
        }

        public static ParticipantSkillProgressDto ToLockedProgressDto(Guid userId, int skillId)
        {
            return new ParticipantSkillProgressDto
            {
                UserId = userId,
                SkillId = skillId,
                Status = SkillStatus.Locked,
                UpdatedAtUtc = null,
                UpdatedById = null
            };
        }
    }
}
