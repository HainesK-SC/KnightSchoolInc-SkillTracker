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
        private readonly IParticipantSkillProgressRepository _participantSkillProgressRepository;

        public SkillService(ILogger<SkillService> logger, ISkillRepository skillRepo, IDisciplineRepository disciplineRepo, IUnitOfWork unitOfWork, IParticipantSkillProgressRepository participantSkillProgressRepository)
        {
            _logger = logger;
            _skillRepository = skillRepo;
            _disciplineRepository = disciplineRepo;
            _unitOfWork = unitOfWork;
            _participantSkillProgressRepository = participantSkillProgressRepository;
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
            var discipline = await _disciplineRepository.GetDisciplineByIdForUpdate(disciplineId);

            if (discipline == null)
            {
                _logger.LogWarning("[SkillService]: DisciplineId: {DisciplineId} not found.", disciplineId);
                return Result<bool>.Failure("No discipline found.", ResultErrorType.NotFound);
            }

            var isProgress = await _participantSkillProgressRepository.AnyProgressInDisciplineAsync(disciplineId);

            if (isProgress)
            {
                return Result<bool>.Failure("This discipline can’t be deleted because participants have progress on its skills. Archive its skills instead to hide it from skill trees.", ResultErrorType.Conflict);
            }

            var skillsInDiscipline = await _skillRepository.GetAllSkillsInDisciplineAsync(disciplineId);

            _skillRepository.RemoveSkills(skillsInDiscipline);
            _disciplineRepository.RemoveDiscipline(discipline);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Discipline: {DisciplineId} has been deleted. There were {SkillsCount} deleted along with the discipline. Performed by Admin: {AdminId}",
                disciplineId, skillsInDiscipline.Count(), actingAdminId);

            return Result<bool>.Success(true);
        }

        // Skills
        public async Task<Result<IReadOnlyList<DisciplineCatalogDto>>> GetCatalogAsync()
        {
            var allDisciplines = await _disciplineRepository.GetAllDisciplinesAsync();

            if (allDisciplines.Count() == 0)
            {
                _logger.LogWarning("No disciplines found.");
                return Result<IReadOnlyList<DisciplineCatalogDto>>.Success(new List<DisciplineCatalogDto>());
            }

            var allSkills = await _skillRepository.GetAllSkillsAsync();

            if (allSkills.Count() == 0)
            {
                _logger.LogWarning("No skills found.");
                return Result<IReadOnlyList<DisciplineCatalogDto>>.Success(new List<DisciplineCatalogDto>());
            }

            var catalogDtos = new List<DisciplineCatalogDto>();

            foreach (var discipline in allDisciplines)
            {
                var catalogDto = new DisciplineCatalogDto
                {
                    DisciplineId = discipline.DisciplineId,
                    Name = discipline.Name,
                    Skills = allSkills.Where(s => s.DisciplineId == discipline.DisciplineId).Select(s => s.ToSkillDto()).ToList()
                };

                catalogDtos.Add(catalogDto);
            }

            return Result<IReadOnlyList<DisciplineCatalogDto>>.Success(catalogDtos);
        }

        public async Task<Result<SkillDto>> GetSkillAsync(int skillId)
        {
            var skill = await _skillRepository.GetSkillByIdAsync(skillId);

            if (skill == null)
            {
                _logger.LogWarning("No skill with ID: {SkillId} found.", skillId);
                return Result<SkillDto>.Failure("No skill found.", ResultErrorType.NotFound);
            }

            var skillDto = skill.ToSkillDto();

            return Result<SkillDto>.Success(skillDto);
        }

        public async Task<Result<SkillDto>> CreateSkillAsync(SkillRequestDto request, Guid actingAdminId)
        {
            var discipline = await _disciplineRepository.GetDisciplineByIdAsync((int) request.DisciplineId);

            if (discipline is null)
            {
                return Result<SkillDto>.Failure(
                    $"Discipline {request.DisciplineId} does not exist.", ResultErrorType.Validation);
            }

            var name = discipline.Name.Trim();
            
            var disciplineIsExist = await _disciplineRepository.DisciplineExistsByName(name);

            if (disciplineIsExist == false)
            {
                _logger.LogWarning("The discipline {DisciplineName} does not exist. ", name);
                return Result<SkillDto>.Failure("Failed to create skill. Discipline does not exist.", ResultErrorType.NotFound);
            }

            if (await _skillRepository.ExistsByNameInDisciplineAsync((int) request.DisciplineId, name) == true)
            {
                _logger.LogWarning("Skill with the name {SkillName} already exists.", name);
                return Result<SkillDto>.Failure("Failed to add skill. Name already in use.", ResultErrorType.Conflict);
            }

            // This prevents empty strings being sent in API calls
            // Forces null
            string? benefits = null;
            if (!string.IsNullOrWhiteSpace(request.Benefits))
            {
                benefits = request.Benefits.Trim();
            }

            var skill = new Skill
            {
                Name = name,
                Description = request.Description.Trim(),
                Benefits = benefits,
                IsArchived = false,
                DisciplineId = (int) request.DisciplineId
            };

            _skillRepository.AddNewSkill(skill);

            var saveResult = await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("New skill: {SkillId} added by Admin: {AdminId}", skill.SkillId, actingAdminId);

            var skillDto = skill.ToSkillDto();

            return Result<SkillDto>.Success(skillDto);
        }

        public async Task<Result<SkillDto>> UpdateSkillAsync(int skillId, SkillRequestDto request, Guid actingAdminId)
        {
            var skillToUpdate = await _skillRepository.GetSkillByIdForUpdate(skillId);

            if (skillToUpdate == null)
            {
                _logger.LogWarning("Skill not found. Check the ID: {SkillId}", skillId);
                return Result<SkillDto>.Failure("Skill not found for update.", ResultErrorType.NotFound);
            }

            var discipline = await _disciplineRepository.GetDisciplineByIdAsync(request.DisciplineId!.Value);

            if (discipline is null)
            {
                return Result<SkillDto>.Failure(
                    $"Discipline {request.DisciplineId} does not exist.", ResultErrorType.Validation);
            }

            var name = request.Name.Trim();

            var isInDiscipline = await _skillRepository.ExistsByNameInDisciplineAsync((int) request.DisciplineId, name, skillToUpdate.SkillId);
            if (isInDiscipline == true)
            {
                _logger.LogWarning("Unable to udpate. Skill name: {SkillName} already in use.", name);
                return Result<SkillDto>.Failure("Failed to update skill. Name already in use.", ResultErrorType.Conflict);
            }

            string? benefits = null;
            if (!string.IsNullOrWhiteSpace(request.Benefits))
            {
                benefits = request.Benefits.Trim();
            }

            skillToUpdate.Name = name;
            skillToUpdate.Description = request.Description.Trim();
            skillToUpdate.Benefits = benefits;
            skillToUpdate.DisciplineId = (int) request.DisciplineId;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Skill {SkillId} has been updated by Admin: {AdminId}", skillToUpdate.SkillId, actingAdminId);
            
            var skillDto = skillToUpdate.ToSkillDto();

            return Result<SkillDto>.Success(skillDto);
        }
        public async Task<Result<SkillDto>> ArchiveSkillAsync(int skillId, Guid actingAdminId)
        {
            var skill = await _skillRepository.GetSkillByIdForUpdate(skillId);

            if (skill == null)
            {
                _logger.LogWarning("Skill: {SkillId} not found.", skillId);
                return Result<SkillDto>.Failure("Skill not found.", ResultErrorType.NotFound);
            }

            if (skill.IsArchived == true)
            {
                _logger.LogWarning("The skill: {SkillName} is already archived.", skill.Name);
                return Result<SkillDto>.Success(skill.ToSkillDto());
            }

            skill.IsArchived = true;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Skill: {SkillId} has been archived by Admin: {AdminId}", skillId, actingAdminId);

            var skillDto = skill.ToSkillDto();
            
            return Result<SkillDto>.Success(skillDto);
        }
        public async Task<Result<SkillDto>> UnarchiveSkillAsync(int skillId, Guid actingAdminId)
        {
            var skill = await _skillRepository.GetSkillByIdForUpdate(skillId);

            if (skill == null)
            {
                _logger.LogWarning("Skill: {SkillId} not found.", skillId);
                return Result<SkillDto>.Failure("Skill not found.", ResultErrorType.NotFound);
            }

            if (skill.IsArchived == false)
            {
                _logger.LogWarning("The skill: {SkillName} is not archived.", skill.Name);
                return Result<SkillDto>.Success(skill.ToSkillDto());
                // return Result<SkillDto>.Failure($"Skill: {skill.Name} is not archived.", ResultErrorType.Conflict);
            }

            skill.IsArchived = false;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Skill: {SkillId} has been retrieved from archive by Admin: {AdminId}", skillId, actingAdminId);

            var skillDto = skill.ToSkillDto();

            return Result<SkillDto>.Success(skillDto);
        }

        public async Task<Result<bool>> DeleteSkillAsync(int skillId, Guid actingAdminId)
        {
            var skill =await _skillRepository.GetSkillByIdForUpdate(skillId);

            if (skill == null)
            {
                return Result<bool>.Failure("No skill found.", ResultErrorType.NotFound);
            }

            var anyProgress = await _participantSkillProgressRepository.AnyProgressForSkillAsync(skillId);

            if (anyProgress)
            {
                return Result<bool>.Failure("Cannot delete skill. Users have progress. Archive instead.", ResultErrorType.Conflict);
            }

            _skillRepository.RemoveSkill(skill);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Skill: {SkillId} deleted. Performed by Admin: {AdminId}", skillId, actingAdminId);

            return Result<bool>.Success(true);
        }
    }
}
