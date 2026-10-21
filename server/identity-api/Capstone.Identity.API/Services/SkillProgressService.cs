using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Mappings;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;

namespace Capstone.Identity.API.Services
{
    public class SkillProgressService : ISkillProgressService
    {
        private readonly ISkillRepository _skillRepository;
        private readonly IParticipantSkillProgressRepository _progressRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<SkillProgressService> _logger;

        public SkillProgressService(
            ISkillRepository skillRepository,
            IParticipantSkillProgressRepository progressRepository,
            IUserProfileRepository userProfileRepository,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider,
            ILogger<SkillProgressService> logger)
        {
            _skillRepository = skillRepository;
            _progressRepository = progressRepository;
            _userProfileRepository = userProfileRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Result<ParticipantSkillProgressDto>> SetSkillStatusAsync(
            Guid userId,
            int skillId,
            SkillStatus status,
            Guid actingUserId)
        {
            // 1. Reject undefined enum values (e.g. a raw number that slipped through)
            if (!Enum.IsDefined(status))
            {
                return Result<ParticipantSkillProgressDto>.Failure("Invalid skill status.", 
                    ResultErrorType.Validation);
            }

            // 2. The skill must exist (archived or not, for now)
            var skill = await _skillRepository.GetSkillByIdAsync(skillId);
            if (skill is null)
            {
                return Result<ParticipantSkillProgressDto>.Failure("Skill not found.", 
                    ResultErrorType.NotFound);
            }

            // 3. Resolve the participant's profile from their user ID
            var userProfile = await _userProfileRepository.GetUserProfileByUserIdAsync(userId);
            if (userProfile is null)
            {
                return Result<ParticipantSkillProgressDto>.Failure("Participant not found.",
                    ResultErrorType.NotFound);
            }

            // 4. Existing progress row, tracked so it can be changed or removed
            var progress = await _progressRepository.GetForUpdateAsync(userProfile.UserProfileId, skillId);

            // 5a. Locked = undo. Allowed even on archived skills.
            if (status == SkillStatus.Locked)
            {
                return await LockAsync(userId, skillId, progress, actingUserId);
            }

            // 5b. Recording progress requires an active skill
            if (skill.IsArchived)
            {
                return Result<ParticipantSkillProgressDto>.Failure("Progress can't be recorded on an archived skill.",
                    ResultErrorType.Validation);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            // No row yet: create one
            if (progress is null)
            {
                var newProgress = new ParticipantSkillProgress
                {
                    UserProfileId = userProfile.UserProfileId,
                    SkillId = skillId,
                    Status = status,
                    UpdatedAtUtc = now,
                    UpdatedById = actingUserId
                };

                _progressRepository.AddNewProgressRow(newProgress);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Skill progress set for user {UserId} on skill {SkillId}: {OldStatus} -> {NewStatus} by {ActingUserId}",
                    userId, skillId, SkillStatus.Locked, status, actingUserId);

                return Result<ParticipantSkillProgressDto>.Success(
                    newProgress.ToParticipantSkillProgressDto(userId));
            }

            // Same status: nothing to do, and the timestamp is left alone
            if (progress.Status == status)
            {
                _logger.LogDebug(
                    "Skill progress unchanged for user {UserId} on skill {SkillId}: already {Status}",
                    userId, skillId, status);

                return Result<ParticipantSkillProgressDto>.Success(
                    progress.ToParticipantSkillProgressDto(userId));
            }

            // Different status: update the tracked row
            var oldStatus = progress.Status;

            progress.Status = status;
            progress.UpdatedAtUtc = now;
            progress.UpdatedById = actingUserId;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Skill progress set for user {UserId} on skill {SkillId}: {OldStatus} -> {NewStatus} by {ActingUserId}",
                userId, skillId, oldStatus, status, actingUserId);

            return Result<ParticipantSkillProgressDto>.Success(
                progress.ToParticipantSkillProgressDto(userId));
        }

        private async Task<Result<ParticipantSkillProgressDto>> LockAsync(
            Guid userId,
            int skillId,
            ParticipantSkillProgress? progress,
            Guid actingUserId)
        {
            var lockedDto = ParticipantSkillProgressMappings.ToLockedProgressDto(userId, skillId);

            // Already Locked (no row): nothing to do
            if (progress is null)
            {
                _logger.LogDebug(
                    "Skill progress unchanged for user {UserId} on skill {SkillId}: already {Status}",
                    userId, skillId, SkillStatus.Locked);

                return Result<ParticipantSkillProgressDto>.Success(lockedDto);
            }

            var oldStatus = progress.Status;

            _progressRepository.RemoveProgressRow(progress);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Skill progress set for user {UserId} on skill {SkillId}: {OldStatus} -> {NewStatus} by {ActingUserId}",
                userId, skillId, oldStatus, SkillStatus.Locked, actingUserId);

            return Result<ParticipantSkillProgressDto>.Success(lockedDto);
        }
    }
}
