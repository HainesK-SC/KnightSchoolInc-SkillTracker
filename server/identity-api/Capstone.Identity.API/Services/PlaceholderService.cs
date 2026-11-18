using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Capstone.Identity.API.Services
{
    public class PlaceholderService : IPlaceholderService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IParticipantSkillProgressRepository _progressRepository;
        private readonly IEventEnrollmentRepository _enrollmentRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PlaceholderService> _logger;

        public PlaceholderService(
            UserManager<ApplicationUser> userManager,
            IUserProfileRepository userProfileRepository,
            IParticipantSkillProgressRepository progressRepository,
            IEventEnrollmentRepository enrollmentRepository,
            IEventRepository eventRepository,
            IUnitOfWork unitOfWork,
            ILogger<PlaceholderService> logger)
        {
            _userManager = userManager;
            _userProfileRepository = userProfileRepository;
            _progressRepository = progressRepository;
            _enrollmentRepository = enrollmentRepository;
            _eventRepository = eventRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<MergePreviewDto>> PreviewMergeAsync(Guid placeholderUserId, Guid targetUserId)
        {
            var planResult = await BuildMergePlanAsync(placeholderUserId, targetUserId);
            if (!planResult.Succeeded)
            {
                return Result<MergePreviewDto>.Failure(planResult.Error!, planResult.ErrorType);
            }

            var plan = planResult.Data!;

            var conflicts = new List<MergeProgressConflictDto>();
            foreach (var conflict in plan.ProgressConflicts)
            {
                var kept = conflict.TargetRow.Status;
                if (conflict.PlaceholderWins)
                {
                    kept = conflict.PlaceholderRow.Status;
                }

                conflicts.Add(new MergeProgressConflictDto
                {
                    SkillId = conflict.PlaceholderRow.SkillId,
                    PlaceholderStatus = conflict.PlaceholderRow.Status,
                    TargetStatus = conflict.TargetRow.Status,
                    KeptStatus = kept
                });
            }

            return Result<MergePreviewDto>.Success(new MergePreviewDto
            {
                Placeholder = ToSummary(plan.PlaceholderUser, plan.PlaceholderProfile),
                Target = ToSummary(plan.TargetUser, plan.TargetProfile),
                ProgressToMove = plan.ProgressToMove.Count,
                ProgressConflicts = conflicts,
                EnrollmentsToMove = plan.EnrollmentsToMove.Count,
                DuplicateEnrollmentsDropped = plan.DuplicateEnrollments.Count,
                OverlappingEventIds = plan.OverlappingEventIds,
                CanMerge = plan.OverlappingEventIds.Count == 0
            });
        }

        public async Task<Result<bool>> MergeAsync(Guid placeholderUserId, Guid targetUserId, Guid actingAdminId)
        {
            var planResult = await BuildMergePlanAsync(placeholderUserId, targetUserId);
            if (!planResult.Succeeded)
            {
                return Result<bool>.Failure(planResult.Error!, planResult.ErrorType);
            }

            var plan = planResult.Data!;

            if (plan.OverlappingEventIds.Count > 0)
            {
                return Result<bool>.Failure(
                    "The placeholder is enrolled in events that overlap the target's events " +
                    $"({string.Join(", ", plan.OverlappingEventIds)}). Unenroll one of them first.",
                    ResultErrorType.Conflict);
            }

            var targetProfileId = plan.TargetProfile.UserProfileId;

            await using (var transaction = await _unitOfWork.BeginTransactionAsync())
            {
                // 1. Progress the target doesn't have: re-point the row to the target
                foreach (var row in plan.ProgressToMove)
                {
                    row.UserProfileId = targetProfileId;
                }

                // 2. Progress both have: keep the winner on the target row, drop the placeholder's
                foreach (var conflict in plan.ProgressConflicts)
                {
                    if (conflict.PlaceholderWins)
                    {
                        conflict.TargetRow.Status = conflict.PlaceholderRow.Status;
                        conflict.TargetRow.UpdatedAtUtc = conflict.PlaceholderRow.UpdatedAtUtc;
                        conflict.TargetRow.UpdatedById = conflict.PlaceholderRow.UpdatedById;
                        conflict.TargetRow.EventId = conflict.PlaceholderRow.EventId;
                    }

                    _progressRepository.RemoveProgressRow(conflict.PlaceholderRow);
                }

                // 3. Enrollments: move the ones the target doesn't have, drop duplicates
                foreach (var enrollment in plan.EnrollmentsToMove)
                {
                    enrollment.UserProfileId = targetProfileId;
                }

                foreach (var duplicate in plan.DuplicateEnrollments)
                {
                    _enrollmentRepository.RemoveEnrollment(duplicate);
                }

                // 4. Save the moves BEFORE deleting the placeholder.
                //    Deleting the account cascades to its profile and anything still attached to it.
                await _unitOfWork.SaveChangesAsync();

                // 5. Delete the placeholder account (same DbContext, so it's inside this transaction)
                var deleteResult = await _userManager.DeleteAsync(plan.PlaceholderUser);
                if (!deleteResult.Succeeded)
                {
                    var errors = string.Join(" ", deleteResult.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Merge of {PlaceholderUserId} into {TargetUserId} failed deleting the placeholder: {Errors}",
                        placeholderUserId, targetUserId, errors);

                    // Leaving the block without committing rolls back step 4 as well
                    return Result<bool>.Failure("The merge could not be completed.", ResultErrorType.Unexpected);
                }

                await transaction.CommitAsync();
            }

            _logger.LogInformation(
                "Placeholder {PlaceholderUserId} merged into {TargetUserId} by admin {AdminId}: " +
                "{MovedProgress} progress moved, {Conflicts} conflicts resolved, " +
                "{MovedEnrollments} enrollments moved, {DroppedEnrollments} duplicates dropped",
                placeholderUserId, targetUserId, actingAdminId,
                plan.ProgressToMove.Count, plan.ProgressConflicts.Count,
                plan.EnrollmentsToMove.Count, plan.DuplicateEnrollments.Count);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeletePlaceholderAsync(Guid placeholderUserId, Guid actingAdminId)
        {
            var placeholder = await GetPlaceholderAsync(placeholderUserId);
            if (!placeholder.Succeeded)
            {
                return Result<bool>.Failure(placeholder.Error!, placeholder.ErrorType);
            }

            var (user, profile) = placeholder.Data!;

            var progress = await _progressRepository.GetForUserAsync(profile.UserProfileId);
            var enrollments = await _enrollmentRepository.GetForProfileForUpdateAsync(profile.UserProfileId);

            if (progress.Count > 0 || enrollments.Count > 0)
            {
                return Result<bool>.Failure(
                    "This placeholder has progress or event enrollments. Merge it into the person's account instead.",
                    ResultErrorType.Conflict);
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return Result<bool>.Failure("The placeholder could not be deleted.", ResultErrorType.Unexpected);
            }

            _logger.LogInformation(
                "Placeholder {PlaceholderUserId} deleted by admin {AdminId}", placeholderUserId, actingAdminId);

            return Result<bool>.Success(true);
        }

        private sealed class ProgressConflict
        {
            public ParticipantSkillProgress PlaceholderRow { get; init; } = null!;
            public ParticipantSkillProgress TargetRow { get; init; } = null!;
            public bool PlaceholderWins { get; init; }
        }

        private sealed class MergePlan
        {
            public ApplicationUser PlaceholderUser { get; init; } = null!;
            public UserProfile PlaceholderProfile { get; init; } = null!;
            public ApplicationUser TargetUser { get; init; } = null!;
            public UserProfile TargetProfile { get; init; } = null!;
            public List<ParticipantSkillProgress> ProgressToMove { get; } = [];
            public List<ProgressConflict> ProgressConflicts { get; } = [];
            public List<EventEnrollment> EnrollmentsToMove { get; } = [];
            public List<EventEnrollment> DuplicateEnrollments { get; } = [];
            public List<int> OverlappingEventIds { get; } = [];
        }

        private async Task<Result<MergePlan>> BuildMergePlanAsync(Guid placeholderUserId, Guid targetUserId)
        {
            if (placeholderUserId == targetUserId)
            {
                return Result<MergePlan>.Failure("An account can't be merged into itself.", ResultErrorType.Validation);
            }

            var placeholder = await GetPlaceholderAsync(placeholderUserId);
            if (!placeholder.Succeeded)
            {
                return Result<MergePlan>.Failure(placeholder.Error!, placeholder.ErrorType);
            }

            var (placeholderUser, placeholderProfile) = placeholder.Data!;

            var targetUser = await _userManager.FindByIdAsync(targetUserId.ToString());
            if (targetUser is null)
            {
                return Result<MergePlan>.Failure("Target account not found.", ResultErrorType.NotFound);
            }

            var targetProfile = await _userProfileRepository.GetUserProfileByUserIdAsync(targetUserId);
            if (targetProfile is null)
            {
                return Result<MergePlan>.Failure("Target account is missing its profile.", ResultErrorType.Unexpected);
            }

            var plan = new MergePlan
            {
                PlaceholderUser = placeholderUser,
                PlaceholderProfile = placeholderProfile,
                TargetUser = targetUser,
                TargetProfile = targetProfile
            };

            // Tracked reads: the merge changes these rows (the preview just never saves)

            var placeholderRows = await _progressRepository.GetForUserForUpdateAsync(placeholderProfile.UserProfileId);
            var targetRows = await _progressRepository.GetForUserForUpdateAsync(targetProfile.UserProfileId);

            var targetRowsBySkillId = new Dictionary<int, ParticipantSkillProgress>();
            foreach (var row in targetRows)
            {
                targetRowsBySkillId.Add(row.SkillId, row);
            }

            foreach (var placeholderRow in placeholderRows)
            {
                if (!targetRowsBySkillId.TryGetValue(placeholderRow.SkillId, out var targetRow))
                {
                    plan.ProgressToMove.Add(placeholderRow);
                    continue;
                }

                plan.ProgressConflicts.Add(new ProgressConflict
                {
                    PlaceholderRow = placeholderRow,
                    TargetRow = targetRow,
                    PlaceholderWins = PlaceholderWins(placeholderRow, targetRow)
                });
            }

            var placeholderEnrollments = await _enrollmentRepository.GetForProfileForUpdateAsync(placeholderProfile.UserProfileId);
            var targetEnrollments = await _enrollmentRepository.GetForProfileForUpdateAsync(targetProfile.UserProfileId);
            var targetEventIds = targetEnrollments.Select(e => e.EventId).ToList();

            foreach (var enrollment in placeholderEnrollments)
            {
                if (targetEventIds.Contains(enrollment.EventId))
                {
                    plan.DuplicateEnrollments.Add(enrollment);
                    continue;
                }

                plan.EnrollmentsToMove.Add(enrollment);

                // Would moving this enrollment double-book the target?
                var enrolledEvent = await _eventRepository.GetEventByIdAsync(enrollment.EventId);
                if (enrolledEvent is not null && !enrolledEvent.IsCancelled)
                {
                    var overlaps = await _enrollmentRepository.HasOverlappingEnrollmentAsync(
                        targetProfile.UserProfileId, enrolledEvent.EventStart, enrolledEvent.EventEnd, enrolledEvent.EventId);

                    if (overlaps)
                    {
                        plan.OverlappingEventIds.Add(enrolledEvent.EventId);
                    }
                }
            }

            return Result<MergePlan>.Success(plan);
        }

        // The account must exist, have no password (a placeholder), and not be staff
        private async Task<Result<(ApplicationUser User, UserProfile Profile)>> GetPlaceholderAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result<(ApplicationUser, UserProfile)>.Failure("Placeholder account not found.", ResultErrorType.NotFound);
            }

            if (user.PasswordHash is not null)
            {
                return Result<(ApplicationUser, UserProfile)>.Failure(
                    "This is a full account with a login, not a placeholder.", ResultErrorType.Validation);
            }

            var isInstructor = await _userManager.IsInRoleAsync(user, Roles.Instructor);
            var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
            if (isInstructor || isAdmin)
            {
                return Result<(ApplicationUser, UserProfile)>.Failure(
                    "Staff accounts can't be merged or deleted here.", ResultErrorType.Validation);
            }

            var profile = await _userProfileRepository.GetUserProfileByUserIdAsync(userId);
            if (profile is null)
            {
                return Result<(ApplicationUser, UserProfile)>.Failure(
                    "Placeholder account is missing its profile.", ResultErrorType.Unexpected);
            }

            return Result<(ApplicationUser, UserProfile)>.Success((user, profile));
        }

        // More advanced status wins; on a tie, the earlier timestamp (first earned) wins
        private static bool PlaceholderWins(ParticipantSkillProgress placeholderRow, ParticipantSkillProgress targetRow)
        {
            var placeholderRank = StatusRank(placeholderRow.Status);
            var targetRank = StatusRank(targetRow.Status);

            if (placeholderRank != targetRank)
            {
                return placeholderRank > targetRank;
            }

            return placeholderRow.UpdatedAtUtc < targetRow.UpdatedAtUtc;
        }

        private static int StatusRank(SkillStatus status)
        {
            if (status == SkillStatus.Completed)
            {
                return 2;
            }

            if (status == SkillStatus.InProgress)
            {
                return 1;
            }

            return 0;
        }

        private static MergeAccountSummaryDto ToSummary(ApplicationUser user, UserProfile profile)
        {
            var status = "Active";
            if (user.Email is null)
            {
                status = "WalkUp";
            }
            else if (user.PasswordHash is null)
            {
                status = "PendingActivation";
            }

            return new MergeAccountSummaryDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = profile.DisplayName,
                Email = user.Email,
                AccountStatus = status
            };
        }
    }
}