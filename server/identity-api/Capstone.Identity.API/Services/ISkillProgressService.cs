using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Services
{
    public interface ISkillProgressService
    {
        Task<Result<ParticipantSkillProgressDto>> SetSkillStatusAsync(
        Guid userId,
        int skillId,
        SkillStatus status,
        Guid actingUserId);
    }
}
