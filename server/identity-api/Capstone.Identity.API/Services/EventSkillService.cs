using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Mappings;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
namespace Capstone.Identity.API.Services
{
    public class EventSkillService : IEventSkillService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IEventSkillRepository _eventSkillRepository;
        private readonly ISkillRepository _skillRepository;
        private readonly IDisciplineRepository _disciplineRepository;
        private readonly IEventStaffRepository _eventStaffRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EventSkillService> _logger;

        public EventSkillService(
            IEventRepository eventRepository,
            IEventSkillRepository eventSkillRepository,
            ISkillRepository skillRepository,
            IDisciplineRepository disciplineRepository,
            IEventStaffRepository eventStaffRepository,
            IUnitOfWork unitOfWork,
            ILogger<EventSkillService> logger)
        {
            _eventRepository = eventRepository;
            _eventSkillRepository = eventSkillRepository;
            _skillRepository = skillRepository;
            _disciplineRepository = disciplineRepository;
            _eventStaffRepository = eventStaffRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<DisciplineCatalogDto>>> SetEventSkillsAsync(
            int eventId,
            SetEventSkillsRequestDto request,
            Guid actingAdminId)
        {
            var existingEvent = await _eventRepository.GetEventByIdAsync(eventId);
            if (existingEvent == null)
            {
                return Result<List<DisciplineCatalogDto>>.Failure("No event found.", ResultErrorType.NotFound);
            }

            var requestedIds = request.SkillIds!.Distinct().ToList();

            // Every requested skill must exist and be active
            var requestedSkills = await _skillRepository.GetSkillsByIdsAsync(requestedIds);

            var foundIds = requestedSkills.Select(s => s.SkillId).ToList();
            var missingIds = requestedIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count > 0)
            {
                return Result<List<DisciplineCatalogDto>>.Failure(
                    $"These skills don't exist: {string.Join(", ", missingIds)}.",
                    ResultErrorType.Validation);
            }

            var archivedIds = requestedSkills.Where(s => s.IsArchived).Select(s => s.SkillId).ToList();
            if (archivedIds.Count > 0)
            {
                return Result<List<DisciplineCatalogDto>>.Failure(
                    $"These skills are archived and can't be added to an event: {string.Join(", ", archivedIds)}.",
                    ResultErrorType.Validation);
            }

            // Wipe the current list, then add the requested one, in one transaction
            var currentRows = await _eventSkillRepository.GetEventSkillsForUpdateAsync(eventId);

            var newRows = new List<EventSkill>();
            foreach (var skillId in requestedIds)
            {
                newRows.Add(new EventSkill { EventId = eventId, SkillId = skillId });
            }

            await using (var transaction = await _unitOfWork.BeginTransactionAsync())
            {
                _eventSkillRepository.RemoveEventSkills(currentRows);
                await _unitOfWork.SaveChangesAsync();

                _eventSkillRepository.AddEventSkills(newRows);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }

            _logger.LogInformation(
                "Event {EventId} skills set to {SkillCount} skills (was {PreviousCount}) by admin {AdminId}",
                eventId, newRows.Count, currentRows.Count, actingAdminId);

            return await GetEventSkillsAsync(eventId, actingAdminId, callerIsAdmin: true);
        }

        public async Task<Result<List<DisciplineCatalogDto>>> GetEventSkillsAsync(
            int eventId,
            Guid callerUserId,
            bool callerIsAdmin)
        {
            var existingEvent = await _eventRepository.GetEventByIdAsync(eventId);
            if (existingEvent == null)
            {
                return Result<List<DisciplineCatalogDto>>.Failure("No event found.", ResultErrorType.NotFound);
            }

            // Instructors can only see events they're assigned to
            // This should almost never be an issue - shouldn't get to this point if not assigned
            if (!callerIsAdmin)
            {
                var isAssigned = await _eventStaffRepository.IsAssignedAsync(eventId, callerUserId);
                if (!isAssigned)
                {
                    return Result<List<DisciplineCatalogDto>>.Failure("Instructors can only view events they are assigned to.", ResultErrorType.NotFound);
                }
            }

            // The event's active skills, plus all disciplines for their names
            var eventSkills = await _eventSkillRepository.GetActiveSkillsForEventAsync(eventId);
            var allDisciplines = await _disciplineRepository.GetAllDisciplinesAsync();

            // Group the skills by discipline to create structure for DisciplineCatalog
            var skillsByDisciplineId = eventSkills
                .GroupBy(s => s.DisciplineId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var result = new List<DisciplineCatalogDto>();

            foreach (var discipline in allDisciplines.OrderBy(d => d.Name))
            {
                // Disciplines with none of this event's skills are left out
                if (!skillsByDisciplineId.TryGetValue(discipline.DisciplineId, out var disciplineSkills))
                {
                    continue;
                }

                var skillDtos = new List<SkillDto>();
                foreach (var skill in disciplineSkills.OrderBy(s => s.Name))
                {
                    skillDtos.Add(skill.ToSkillDto());
                }

                result.Add(new DisciplineCatalogDto
                {
                    DisciplineId = discipline.DisciplineId,
                    Name = discipline.Name,
                    Skills = skillDtos
                });
            }

            return Result<List<DisciplineCatalogDto>>.Success(result);
        }
    }
}
