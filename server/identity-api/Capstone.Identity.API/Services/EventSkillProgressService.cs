using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Mappings;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;

namespace Capstone.Identity.API.Services
{
    public class EventSkillProgressService : IEventSkillProgressService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IEventStaffRepository _staffRepository;
        private readonly IEventEnrollmentRepository _enrollmentRepository;
        private readonly IEventSkillRepository _eventSkillRepository;
        private readonly IDisciplineRepository _disciplineRepository;
        private readonly IParticipantSkillProgressRepository _progressRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<EventSkillProgressService> _logger;

        public EventSkillProgressService(
            IEventRepository eventRepository,
            IEventStaffRepository staffRepository,
            IEventEnrollmentRepository enrollmentRepository,
            IEventSkillRepository eventSkillRepository,
            IDisciplineRepository disciplineRepository,
            IParticipantSkillProgressRepository progressRepository,
            IUserProfileRepository userProfileRepository,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider,
            ILogger<EventSkillProgressService> logger)
        {
            _eventRepository = eventRepository;
            _staffRepository = staffRepository;
            _enrollmentRepository = enrollmentRepository;
            _eventSkillRepository = eventSkillRepository;
            _disciplineRepository = disciplineRepository;
            _progressRepository = progressRepository;
            _userProfileRepository = userProfileRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Result<SkillTreeDto>> GetParticipantEventSkillsAsync(
            int eventId, Guid userId, Guid callerUserId, bool callerIsAdmin)
        {
            var access = await GetManageableEventAsync(eventId, callerUserId, callerIsAdmin);
            if (!access.Succeeded)
            {
                return Result<SkillTreeDto>.Failure(access.Error!, access.ErrorType);
            }

            var participant = await GetEnrolledParticipantAsync(eventId, userId);
            if (!participant.Succeeded)
            {
                return Result<SkillTreeDto>.Failure(participant.Error!, participant.ErrorType);
            }

            var eventSkills = await _eventSkillRepository.GetActiveSkillsForEventAsync(eventId);
            var disciplines = await _disciplineRepository.GetAllDisciplinesAsync();
            var progressRows = await _progressRepository.GetForUserAsync(participant.Data!.UserProfileId);

            var tree = BuildTree(userId, eventSkills, disciplines, progressRows);

            return Result<SkillTreeDto>.Success(tree);
        }

        public async Task<Result<SkillTreeDto>> SetEventSkillStatusesAsync(
            int eventId, Guid userId, SetEventSkillStatusesRequestDto request, Guid callerUserId, bool callerIsAdmin)
        {

            var access = await GetManageableEventAsync(eventId, callerUserId, callerIsAdmin);
            if (!access.Succeeded)
            {
                return Result<SkillTreeDto>.Failure(access.Error!, access.ErrorType);
            }

            if (access.Data!.IsCancelled)
            {
                return Result<SkillTreeDto>.Failure(
                    "Progress can't be recorded for a cancelled event.", ResultErrorType.Validation);
            }

            var participant = await GetEnrolledParticipantAsync(eventId, userId);
            if (!participant.Succeeded)
            {
                return Result<SkillTreeDto>.Failure(participant.Error!, participant.ErrorType);
            }

            var profileId = participant.Data!.UserProfileId;

            var changes = request.Changes!;

            var duplicateIds = changes
                .GroupBy(c => c.SkillId!.Value)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicateIds.Count > 0)
            {
                return Result<SkillTreeDto>.Failure(
                    $"Each skill can only appear once. Repeated: {string.Join(", ", duplicateIds)}.",
                    ResultErrorType.Validation);
            }

            var invalidStatusIds = changes
                .Where(c => !Enum.IsDefined(c.Status!.Value))
                .Select(c => c.SkillId!.Value)
                .ToList();
            if (invalidStatusIds.Count > 0)
            {
                return Result<SkillTreeDto>.Failure(
                    $"Invalid status for skills: {string.Join(", ", invalidStatusIds)}.",
                    ResultErrorType.Validation);
            }

            // Only this event's active skills can be marked here (archived ones aren't in the list)
            var eventSkills = await _eventSkillRepository.GetActiveSkillsForEventAsync(eventId);
            var eventSkillIds = eventSkills.Select(s => s.SkillId).ToList();

            var notInEventIds = changes
                .Where(c => !eventSkillIds.Contains(c.SkillId!.Value))
                .Select(c => c.SkillId!.Value)
                .ToList();
            if (notInEventIds.Count > 0)
            {
                return Result<SkillTreeDto>.Failure(
                    $"These skills aren't part of this event: {string.Join(", ", notInEventIds)}.",
                    ResultErrorType.Validation);
            }


            var currentRows = await _progressRepository.GetForUserForUpdateAsync(profileId);

            var rowsBySkillId = new Dictionary<int, ParticipantSkillProgress>();
            foreach (var row in currentRows)
            {
                rowsBySkillId.Add(row.SkillId, row);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var changedCount = 0;

            foreach (var change in changes)
            {
                var skillId = change.SkillId!.Value;
                var status = change.Status!.Value;

                rowsBySkillId.TryGetValue(skillId, out ParticipantSkillProgress? existing);

                // Locked = undo: remove the row if there is one
                if (status == SkillStatus.Locked)
                {
                    if (existing is not null)
                    {
                        _progressRepository.RemoveProgressRow(existing);
                        changedCount++;
                    }

                    continue;
                }

                // No row yet: create one
                if (existing is null)
                {
                    _progressRepository.AddNewProgressRow(new ParticipantSkillProgress
                    {
                        UserProfileId = profileId,
                        SkillId = skillId,
                        Status = status,
                        UpdatedAtUtc = now,
                        UpdatedById = callerUserId,
                        EventId = eventId
                    });
                    changedCount++;

                    continue;
                }

                // Same status: nothing to do, the original timestamp stays
                if (existing.Status == status)
                {
                    continue;
                }

                // Different status: update the tracked row
                existing.Status = status;
                existing.UpdatedAtUtc = now;
                existing.UpdatedById = callerUserId;
                existing.EventId = eventId;
                changedCount++;
            }

            // One save for the whole batch, and only if something actually changed
            if (changedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Event {EventId}: {ChangedCount} skill status changes for user {UserId} by {ActingUserId}",
                    eventId, changedCount, userId, callerUserId);
            }

            // Return the fresh view so the screen shows exactly what's saved
            var progressRows = await _progressRepository.GetForUserAsync(profileId);
            var disciplines = await _disciplineRepository.GetAllDisciplinesAsync();

            var tree = BuildTree(userId, eventSkills, disciplines, progressRows);

            return Result<SkillTreeDto>.Success(tree);
        }

        private async Task<Result<Event>> GetManageableEventAsync(int eventId, Guid callerUserId, bool callerIsAdmin)
        {
            var targetEvent = await _eventRepository.GetEventByIdAsync(eventId);
            if (targetEvent is null)
            {
                return Result<Event>.Failure("No event found.", ResultErrorType.NotFound);
            }

            if (!callerIsAdmin)
            {
                var isAssigned = await _staffRepository.IsAssignedAsync(eventId, callerUserId);
                if (!isAssigned)
                {
                    return Result<Event>.Failure("No event found.", ResultErrorType.NotFound);
                }
            }

            return Result<Event>.Success(targetEvent);
        }

        // The participant must exist and be enrolled in this event
        private async Task<Result<UserProfile>> GetEnrolledParticipantAsync(int eventId, Guid userId)
        {
            var profile = await _userProfileRepository.GetUserProfileByUserIdAsync(userId);
            if (profile is null)
            {
                return Result<UserProfile>.Failure("Participant not found.", ResultErrorType.NotFound);
            }

            var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(eventId, profile.UserProfileId);
            if (!isEnrolled)
            {
                return Result<UserProfile>.Failure(
                    "This participant isn't enrolled in this event.", ResultErrorType.NotFound);
            }

            return Result<UserProfile>.Success(profile);
        }

        // Same grouping as SkillTreeService, but limited to the event's skills
        private static SkillTreeDto BuildTree(
            Guid userId,
            IReadOnlyList<Skill> skills,
            IReadOnlyList<Discipline> disciplines,
            IReadOnlyList<ParticipantSkillProgress> progressRows)
        {
            var progressBySkillId = new Dictionary<int, ParticipantSkillProgress>();
            foreach (var row in progressRows)
            {
                progressBySkillId.Add(row.SkillId, row);
            }

            var skillsByDisciplineId = skills
                .GroupBy(s => s.DisciplineId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var disciplineDtos = new List<SkillTreeDisciplineDto>();

            foreach (var discipline in disciplines.OrderBy(d => d.Name))
            {
                if (!skillsByDisciplineId.TryGetValue(discipline.DisciplineId, out var disciplineSkills))
                {
                    continue;
                }

                var skillDtos = new List<SkillTreeSkillDto>();
                foreach (var skill in disciplineSkills.OrderBy(s => s.Name))
                {
                    progressBySkillId.TryGetValue(skill.SkillId, out ParticipantSkillProgress? progress);
                    skillDtos.Add(skill.ToSkillTreeSkillDto(progress));
                }

                disciplineDtos.Add(new SkillTreeDisciplineDto
                {
                    DisciplineId = discipline.DisciplineId,
                    Name = discipline.Name,
                    Skills = skillDtos
                });
            }

            return new SkillTreeDto
            {
                UserId = userId,
                Disciplines = disciplineDtos
            };
        }
    }
}