using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Response;

namespace Capstone.Identity.API.Services
{
    public interface ISkillTreeService
    {
        public Task<Result<SkillTreeDto>> GetSkillTreeAsync(Guid userId);
    }
}
