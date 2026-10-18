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
            var allDisciplineDtos = new List<DisciplineDto>();

            if (allDisciplines.Count() == 0)
            {
                _logger.LogWarning("[SkillService]: No Disciplines found." +
                    " If disciplines have been added, this is a critical error.");
                return Result<IReadOnlyList<DisciplineDto>>.Success(allDisciplineDtos);
            }

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
                _logger.LogWarning("[SkillService]: No discipline with ID: {DisciplineId} found.", disciplineId);
                return Result<DisciplineDto>.Failure("No discipline found.", ResultErrorType.NotFound);
            }

            return Result<DisciplineDto>.Success(discipline.ToDisciplineDto());
        }
        public async Task<Result<DisciplineDto>> CreateDisciplineAsync(DisciplineRequestDto request, Guid actingAdminId)
        {
            var name = request.Name.Trim();

            if (request == null)
            {
                _logger.LogWarning("[SkillService]: Incoming DisciplineRequestDto is null.");
                return Result<DisciplineDto>.Failure("Failed to retrieve values. Try again.", ResultErrorType.Validation); // if this happens, something on the client side is broken
            }

            if (name == "")
            {
                _logger.LogWarning("[SkillService]: Discipline Name is empty.");
                return Result<DisciplineDto>.Failure("Discipline Name is empty", ResultErrorType.Validation);
            }

            if (await _disciplineRepository.DisciplineExistsByName(name) == true )
            {
                _logger.LogError("[SkillService]: Failed to create discipline.A discipline with name: {DisciplineName} already exists.", request.Name);
                return Result<DisciplineDto>.Failure("Failed to create discipline. Duplicate entry.", ResultErrorType.Conflict);
            }

            var discipline = new Discipline
            {
                Name = request.Name
            };

            _disciplineRepository.AddNewDiscipline(discipline);

            var saveOperation = await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[SkillService]: New Discipline Added. ID: {DisciplineId}. By admin {AdminId}", discipline.DisciplineId, actingAdminId);

            return Result<DisciplineDto>.Success(discipline.ToDisciplineDto());
        }
        public async Task<Result<DisciplineDto>> UpdateDisciplineAsync(int disciplineId, DisciplineRequestDto request, Guid actingAdminId)
        {
            if (request == null)
            {
                _logger.LogWarning("[SkillService]: Incoming DisciplineRequestDto is null.");
                return Result<DisciplineDto>.Failure("Failed to retrieve values. Try again.", ResultErrorType.Unexpected);
            }

            var newName = request.Name.Trim();

            if (newName == "")
            {
                _logger.LogWarning("[SkillService]: Discipline Name is empty.");
                return Result<DisciplineDto>.Failure("Discipline Name is empty", ResultErrorType.Validation);
            }

            if (await _disciplineRepository.DisciplineExistsByName(newName, disciplineId) == true)
            {
                _logger.LogWarning("[SkillService]: Failed to update discipline.A discipline with name: {DisciplineName} already exists.", request.Name);
                return Result<DisciplineDto>.Failure("Failed to update discipline. Duplicate entry.", ResultErrorType.Conflict);
            }

            var disciplineUpdate = await _disciplineRepository.GetDisciplineByIdForUpdate(disciplineId);

            if (disciplineUpdate == null)
            {
                _logger.LogWarning("[SkillService]: No discipline found with ID: {DisciplineId}", disciplineId);
                return Result<DisciplineDto>.Failure("No discipline found.", ResultErrorType.NotFound);
            }

            var oldName = disciplineUpdate.Name;

            disciplineUpdate.Name = newName;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[SkillService]: DisciplineId: {DisciplineId} changed Name property from {OldName} to {NewName} by AdminId: {AdminId}", 
                disciplineId, oldName, newName, actingAdminId);

            return Result<DisciplineDto>.Success(disciplineUpdate.ToDisciplineDto());
        }

        public async Task<Result<bool>> DeleteDisciplineAsync(int disciplineId, Guid actingAdminId)
        {
            var discipline = await _disciplineRepository.GetDisciplineByIdAsync(disciplineId);

            if (discipline == null)
            {
                _logger.LogWarning("[SkillService]: DisciplineId: {DisciplineId} not found.", disciplineId);
                return Result<bool>.Failure("No discipline found.", ResultErrorType.NotFound);
            }

            // Checking if skills exist under this Discipline
            // If so, the discipline cannot be delted - skills must be deleted first
            var doSkillsExist = await _disciplineRepository.AnyInDisciplineAsync(disciplineId);

            if (doSkillsExist == true)
            {
                _logger.LogWarning("[SkillService]: This discipline has skills (including archived ones) and can't be deleted. Archive its skills to hide it from skill trees.");
                return Result<bool>.Failure("Cannot delete a discipline that has skills associated with it. Skills must be deleted first.", ResultErrorType.Conflict);
            }

            _disciplineRepository.RemoveDiscipline(discipline);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[SkillService]: DisciplineId: {DisciplineId} has been successfully deleted by AdminId: {AdminId}", disciplineId, actingAdminId);

            return Result<bool>.Success(true);
        }

        // Skills
        public async Task<Result<IReadOnlyList<DisciplineCatalogDto>>> GetCatalogAsync();
        public async Task<Result<SkillDto>> GetSkillAsync(int skillId);
        public async Task<Result<SkillDto>> CreateSkillAsync(SkillRequestDto request, Guid actingAdminId);
        public async Task<Result<SkillDto>> UpdateSkillAsync(int skillId, SkillRequestDto request, Guid actingAdminId);
        public async Task<Result<SkillDto>> ArchiveSkillAsync(int skillId, Guid actingAdminId);
        public async Task<Result<SkillDto>> UnarchiveSkillAsync(int skillId, Guid actingAdminId);
    }
}
