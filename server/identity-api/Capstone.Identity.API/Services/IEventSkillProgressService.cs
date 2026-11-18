using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;

namespace Capstone.Identity.API.Services
{
    public interface IEventSkillProgressService
    {
        Task<Result<SkillTreeDto>> GetParticipantEventSkillsAsync(
            int eventId, Guid userId, Guid callerUserId, bool callerIsAdmin);

        Task<Result<SkillTreeDto>> SetEventSkillStatusesAsync(
            int eventId, Guid userId, SetEventSkillStatusesRequestDto request, Guid callerUserId, bool callerIsAdmin);
    }
}