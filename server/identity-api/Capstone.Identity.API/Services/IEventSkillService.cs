using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Services
{
    public interface IEventSkillService
    {
        Task<Result<List<DisciplineCatalogDto>>> SetEventSkillsAsync(int eventId, SetEventSkillsRequestDto request, Guid actingAdminId);
        Task<Result<List<DisciplineCatalogDto>>> GetEventSkillsAsync(int eventId, Guid callerUserId, bool callerIsAdmin);
    }
}
