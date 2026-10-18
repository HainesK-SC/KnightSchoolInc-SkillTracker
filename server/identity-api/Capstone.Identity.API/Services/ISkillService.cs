using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;

namespace Capstone.Identity.API.Services
{
    // Both Discipline and Skill related methods have been lumped into this service.
    // While there is an argument to separate the two, I don't feel it is worth it.
    // I don't think Disciplines on their own have enough value or use to separate.
    public interface ISkillService
    {
        // Disciplines
        Task<Result<IReadOnlyList<DisciplineDto>>> GetDisciplinesAsync();
        Task<Result<DisciplineDto>> GetDisciplineAsync(int disciplineId);
        Task<Result<DisciplineDto>> CreateDisciplineAsync(DisciplineRequestDto request, Guid actingAdminId);
        Task<Result<DisciplineDto>> UpdateDisciplineAsync(int disciplineId, DisciplineRequestDto request, Guid actingAdminId);
        Task<Result<bool>> DeleteDisciplineAsync(int disciplineId, Guid actingAdminId);

        // Skills
        Task<Result<IReadOnlyList<DisciplineCatalogDto>>> GetCatalogAsync();
        Task<Result<SkillDto>> GetSkillAsync(int skillId);
        Task<Result<SkillDto>> CreateSkillAsync(SkillRequestDto request, Guid actingAdminId);
        Task<Result<SkillDto>> UpdateSkillAsync(int skillId, SkillRequestDto request, Guid actingAdminId);
        Task<Result<SkillDto>> ArchiveSkillAsync(int skillId, Guid actingAdminId);
        Task<Result<SkillDto>> UnarchiveSkillAsync(int skillId, Guid actingAdminId);
    }
}
