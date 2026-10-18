using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Mappings;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;

namespace Capstone.Identity.API.Services
{
    public class SkillService : ISkillService
    {
        private readonly ILogger<SkillService> _logger;
        private readonly ISkillRepository _skillRepository;
        private readonly IDisciplineRepository _disciplineRepository;
        private IUnitOfWork _unitOfWork;

        public SkillService(ILogger<SkillService> logger, ISkillRepository skillRepo, IDisciplineRepository disciplineRepo, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _skillRepository = skillRepo;
            _disciplineRepository = disciplineRepo;
            _unitOfWork = unitOfWork;
        }

        // Disciplines
        public async Task<Result<IReadOnlyList<DisciplineDto>>> GetDisciplinesAsync()
        {
            var allDisciplines = await _disciplineRepository.GetAllDisciplinesAsync();

            if (allDisciplines == null || allDisciplines.Count() == 0)
            {
                _logger.LogWarning("[SkillService]: No Disciplines found." +
                    " If disciplines have been added, this is a critical error.");
                return Result<IReadOnlyList<DisciplineDto>>.Failure("No disciplines found", ResultErrorType.NotFound);
            }

            var allDisciplineDtos = new List<DisciplineDto>();

            foreach (var d in allDisciplines)
            {
                allDisciplineDtos.Add(d.ToDisciplineDto());
            }

            return Result<IReadOnlyList<DisciplineDto>>.Success(allDisciplineDtos);
        }
        public async Task<Result<DisciplineDto>> GetDisciplineAsync(int disciplineId)
        {
            var discipline = await _disciplineRepository.GetDisciplineByIdAsync(disciplineId);

            if (discipline == null)
            {
                _logger.LogWarning("No discipline with ID: {} found.", disciplineId);
                return Result<DisciplineDto>.Failure("No discipline found.", ResultErrorType.NotFound);
            }

            return Result<DisciplineDto>.Success(discipline.ToDisciplineDto());
        }
        public async Task<Result<DisciplineDto>> CreateDisciplineAsync(DisciplineRequestDto request, Guid actingAdminId)
        {
            _logger.LogInformation("[SkillService]: CreateDisciplineAsync method called by AdminId: {}", actingAdminId); ;

            if (request == null)
            {
                _logger.LogWarning("[SkillService]: Incoming DisciplineRequestDto is null.");
                return Result<DisciplineDto>.Failure("Failed to retrieve values. Try again.", ResultErrorType.Validation);
            }

            if (request.Name.Trim() == "")
            {
                _logger.LogError("[SkillService]: Discipline Name is empty.");
                return Result<DisciplineDto>.Failure("Discipline Name is empty", ResultErrorType.Validation);
            }

            if (await _disciplineRepository.DisciplineExistsByName(request.Name) == true )
            {
                _logger.LogError("[SkillService]: Failed to create discipline.A discipline with name: {} already exists.", request.Name);
                Result<DisciplineDto>.Failure("Failed to create discipline. Duplicate entry.", ResultErrorType.Conflict);
            }

            var discipline = new Discipline
            {
                Name = request.Name
            };

            _disciplineRepository.AddNewDiscipline(discipline);

            var saveOperation = await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[SkillService]: New Discipline Added. ID: {}. By admin {}", discipline.DisciplineId, actingAdminId);

            return Result<DisciplineDto>.Success(discipline.ToDisciplineDto());
        }
        public async Task<Result<DisciplineDto>> UpdateDisciplineAsync(int disciplineId, DisciplineRequestDto request, Guid actingAdminId)
        {

        }
        public async Task<Result<bool>> DeleteDisciplineAsync(int disciplineId, Guid actingAdminId);

        // Skills
        public async Task<Result<IReadOnlyList<DisciplineCatalogDto>>> GetCatalogAsync();
        public async Task<Result<SkillDto>> GetSkillAsync(int skillId);
        public async Task<Result<SkillDto>> CreateSkillAsync(SkillRequestDto request, Guid actingAdminId);
        public async Task<Result<SkillDto>> UpdateSkillAsync(int skillId, SkillRequestDto request, Guid actingAdminId);
        public async Task<Result<SkillDto>> ArchiveSkillAsync(int skillId, Guid actingAdminId);
        public async Task<Result<SkillDto>> UnarchiveSkillAsync(int skillId, Guid actingAdminId);
    }
}
