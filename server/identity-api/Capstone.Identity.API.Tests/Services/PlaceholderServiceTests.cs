using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Capstone.Identity.API.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class PlaceholderServiceTests
    {
        private const int PlaceholderProfileId = 1;
        private const int TargetProfileId = 2;

        private static readonly Guid PlaceholderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid TargetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid AdminId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid PlaceholderMarker = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid TargetMarker = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private static readonly DateTime Earlier = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Later = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        private Mock<UserManager<ApplicationUser>> _userManager = null!;
        private Mock<IUserProfileRepository> _profileRepository = null!;
        private Mock<IParticipantSkillProgressRepository> _progressRepository = null!;
        private Mock<IEventEnrollmentRepository> _enrollmentRepository = null!;
        private Mock<IEventRepository> _eventRepository = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IDbContextTransaction> _transaction = null!;
        private PlaceholderService _service = null!;

        private ApplicationUser _placeholder = null!;
        private ApplicationUser _target = null!;

        private List<ParticipantSkillProgress> _placeholderRows = [];
        private List<ParticipantSkillProgress> _targetRows = [];
        private List<EventEnrollment> _placeholderEnrollments = [];
        private List<EventEnrollment> _targetEnrollments = [];
        private Dictionary<int, Event> _events = [];

        private List<ParticipantSkillProgress> _removedRows = [];
        private List<EventEnrollment> _removedEnrollments = [];
        private List<string> _callOrder = [];

        [TestInitialize]
        public void TestInitialize()
        {
            _placeholderRows = [];
            _targetRows = [];
            _placeholderEnrollments = [];
            _targetEnrollments = [];
            _events = [];
            _removedRows = [];
            _removedEnrollments = [];
            _callOrder = [];

            // Placeholder: no password, no email (walk-up). Target: full account.
            _placeholder = new ApplicationUser { Id = PlaceholderId, FirstName = "Kyle", LastName = "Walkup", Email = null, PasswordHash = null };
            _target = new ApplicationUser { Id = TargetId, FirstName = "Kyle", LastName = "Test", Email = "kyle@test.com", PasswordHash = "hashed" };

            _userManager = IdentityMocks.CreateUserManager();
            _userManager.Setup(m => m.FindByIdAsync(PlaceholderId.ToString())).ReturnsAsync(() => _placeholder);
            _userManager.Setup(m => m.FindByIdAsync(TargetId.ToString())).ReturnsAsync(() => _target);
            _userManager.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(false);
            _userManager
                .Setup(m => m.DeleteAsync(It.IsAny<ApplicationUser>()))
                .Callback(() => _callOrder.Add("Delete"))
                .ReturnsAsync(IdentityResult.Success);

            _profileRepository = new Mock<IUserProfileRepository>();
            _profileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(PlaceholderId))
                .ReturnsAsync(new UserProfile { UserProfileId = PlaceholderProfileId, ApplicationUserId = PlaceholderId, DisplayName = "Knight Kyle the Brave" });
            _profileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(TargetId))
                .ReturnsAsync(new UserProfile { UserProfileId = TargetProfileId, ApplicationUserId = TargetId, DisplayName = "Sir Kyle the Bold" });

            _progressRepository = new Mock<IParticipantSkillProgressRepository>();
            _progressRepository.Setup(r => r.GetForUserForUpdateAsync(PlaceholderProfileId)).ReturnsAsync(() => _placeholderRows);
            _progressRepository.Setup(r => r.GetForUserForUpdateAsync(TargetProfileId)).ReturnsAsync(() => _targetRows);
            _progressRepository.Setup(r => r.GetForUserAsync(PlaceholderProfileId)).ReturnsAsync(() => _placeholderRows);
            _progressRepository
                .Setup(r => r.RemoveProgressRow(It.IsAny<ParticipantSkillProgress>()))
                .Callback<ParticipantSkillProgress>(row => _removedRows.Add(row));

            _enrollmentRepository = new Mock<IEventEnrollmentRepository>();
            _enrollmentRepository.Setup(r => r.GetForProfileForUpdateAsync(PlaceholderProfileId)).ReturnsAsync(() => _placeholderEnrollments);
            _enrollmentRepository.Setup(r => r.GetForProfileForUpdateAsync(TargetProfileId)).ReturnsAsync(() => _targetEnrollments);
            _enrollmentRepository
                .Setup(r => r.HasOverlappingEnrollmentAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            _enrollmentRepository
                .Setup(r => r.RemoveEnrollment(It.IsAny<EventEnrollment>()))
                .Callback<EventEnrollment>(e => _removedEnrollments.Add(e));

            _eventRepository = new Mock<IEventRepository>();
            _eventRepository
                .Setup(r => r.GetEventByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    _events.TryGetValue(id, out var found);
                    return found;
                });

            _transaction = new Mock<IDbContextTransaction>();
            _transaction
                .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
                .Callback(() => _callOrder.Add("Commit"))
                .Returns(Task.CompletedTask);

            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_transaction.Object);
            _unitOfWork
                .Setup(u => u.SaveChangesAsync())
                .Callback(() => _callOrder.Add("Save"))
                .ReturnsAsync(1);

            _service = new PlaceholderService(
                _userManager.Object,
                _profileRepository.Object,
                _progressRepository.Object,
                _enrollmentRepository.Object,
                _eventRepository.Object,
                _unitOfWork.Object,
                NullLogger<PlaceholderService>.Instance);
        }

        // ---------- Helpers ----------

        private static ParticipantSkillProgress Row(int profileId, int skillId, SkillStatus status, DateTime at, Guid marker, int? eventId)
        {
            return new ParticipantSkillProgress
            {
                UserProfileId = profileId,
                SkillId = skillId,
                Status = status,
                UpdatedAtUtc = at,
                UpdatedById = marker,
                EventId = eventId
            };
        }

        private ParticipantSkillProgress PlaceholderRow(int skillId, SkillStatus status, DateTime? at = null)
        {
            var row = Row(PlaceholderProfileId, skillId, status, at ?? Earlier, PlaceholderMarker, 900);
            _placeholderRows.Add(row);
            return row;
        }

        private ParticipantSkillProgress TargetRow(int skillId, SkillStatus status, DateTime? at = null)
        {
            var row = Row(TargetProfileId, skillId, status, at ?? Later, TargetMarker, 800);
            _targetRows.Add(row);
            return row;
        }

        private EventEnrollment PlaceholderEnrollment(int eventId, bool cancelled = false)
        {
            _events[eventId] = new Event
            {
                EventId = eventId,
                EventName = $"Event {eventId}",
                EventLocation = "Toronto",
                EventStart = new DateTime(2026, 12, eventId % 28 + 1, 15, 0, 0, DateTimeKind.Utc),
                EventEnd = new DateTime(2026, 12, eventId % 28 + 1, 22, 0, 0, DateTimeKind.Utc),
                IsCancelled = cancelled
            };

            var enrollment = new EventEnrollment { Id = eventId * 10, EventId = eventId, UserProfileId = PlaceholderProfileId, EnrolledAtUtc = Earlier, EnrolledById = AdminId };
            _placeholderEnrollments.Add(enrollment);
            return enrollment;
        }

        private EventEnrollment TargetEnrollment(int eventId)
        {
            var enrollment = new EventEnrollment { Id = eventId * 10 + 1, EventId = eventId, UserProfileId = TargetProfileId, EnrolledAtUtc = Later, EnrolledById = AdminId };
            _targetEnrollments.Add(enrollment);
            return enrollment;
        }

        private void GivenOverlapFor(int eventId)
        {
            _enrollmentRepository
                .Setup(r => r.HasOverlappingEnrollmentAsync(TargetProfileId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), eventId))
                .ReturnsAsync(true);
        }

        private void VerifyNothingWritten()
        {
            _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
            _userManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
            Assert.AreEqual(0, _removedRows.Count);
            Assert.AreEqual(0, _removedEnrollments.Count);
        }

        // =====================================================================
        // Who can be merged
        // =====================================================================

        [TestMethod]
        public async Task Merge_IntoItself_ReturnsValidation()
        {
            var result = await _service.MergeAsync(PlaceholderId, PlaceholderId, AdminId);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNothingWritten();
        }

        [TestMethod]
        public async Task Merge_PlaceholderMissing_ReturnsNotFound()
        {
            _userManager.Setup(m => m.FindByIdAsync(PlaceholderId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNothingWritten();
        }

        [TestMethod]
        public async Task Merge_SourceIsAFullAccount_ReturnsValidation_AndTouchesNothing()
        {
            // The most important guard: a real account with a login is never merged away
            _placeholder.PasswordHash = "hashed";

            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNothingWritten();
        }

        [TestMethod]
        [DataRow(Roles.Instructor)]
        [DataRow(Roles.Admin)]
        public async Task Merge_PlaceholderWithStaffRole_ReturnsValidation(string role)
        {
            _userManager.Setup(m => m.IsInRoleAsync(_placeholder, role)).ReturnsAsync(true);

            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNothingWritten();
        }

        [TestMethod]
        public async Task Merge_PendingActivationPlaceholder_IsAllowed()
        {
            // Has an email but never set a password: still a placeholder
            _placeholder.Email = "walkup@test.com";

            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public async Task Merge_TargetMissing_ReturnsNotFound()
        {
            _userManager.Setup(m => m.FindByIdAsync(TargetId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNothingWritten();
        }

        // =====================================================================
        // Preview
        // =====================================================================

        [TestMethod]
        public async Task Preview_SummarisesEverything_AndWritesNothing()
        {
            PlaceholderRow(10, SkillStatus.InProgress);                    // moves
            PlaceholderRow(11, SkillStatus.Completed);                     // conflict, placeholder wins
            TargetRow(11, SkillStatus.InProgress);
            PlaceholderEnrollment(3);                                      // moves
            PlaceholderEnrollment(4);                                      // duplicate
            TargetEnrollment(4);

            var result = await _service.PreviewMergeAsync(PlaceholderId, TargetId);

            var dto = result.Data!;
            Assert.AreEqual(PlaceholderId, dto.Placeholder.UserId);
            Assert.AreEqual("WalkUp", dto.Placeholder.AccountStatus);
            Assert.AreEqual(TargetId, dto.Target.UserId);
            Assert.AreEqual("Active", dto.Target.AccountStatus);
            Assert.AreEqual(1, dto.ProgressToMove);
            Assert.AreEqual(1, dto.ProgressConflicts.Count);
            Assert.AreEqual(SkillStatus.Completed, dto.ProgressConflicts[0].KeptStatus);
            Assert.AreEqual(1, dto.EnrollmentsToMove);
            Assert.AreEqual(1, dto.DuplicateEnrollmentsDropped);
            Assert.IsTrue(dto.CanMerge);

            VerifyNothingWritten();
        }

        [TestMethod]
        public async Task Preview_OverlappingEvent_CanMergeIsFalse_AndListsTheEvent()
        {
            PlaceholderEnrollment(3);
            GivenOverlapFor(3);

            var result = await _service.PreviewMergeAsync(PlaceholderId, TargetId);

            Assert.IsFalse(result.Data!.CanMerge);
            CollectionAssert.AreEqual(new[] { 3 }, result.Data.OverlappingEventIds);
        }

        [TestMethod]
        public async Task Preview_CancelledEventEnrollment_IsNotCheckedForOverlap()
        {
            PlaceholderEnrollment(3, cancelled: true);
            GivenOverlapFor(3);

            var result = await _service.PreviewMergeAsync(PlaceholderId, TargetId);

            Assert.IsTrue(result.Data!.CanMerge);
            Assert.AreEqual(1, result.Data.EnrollmentsToMove);
        }

        // =====================================================================
        // Conflict rule
        // =====================================================================

        [TestMethod]
        public async Task Conflict_PlaceholderMoreAdvanced_WinsAndIsCopiedOntoTargetRow()
        {
            var placeholderRow = PlaceholderRow(11, SkillStatus.Completed, Later);
            var targetRow = TargetRow(11, SkillStatus.InProgress, Earlier);

            await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(SkillStatus.Completed, targetRow.Status);
            Assert.AreEqual(Later, targetRow.UpdatedAtUtc);
            Assert.AreEqual(PlaceholderMarker, targetRow.UpdatedById);
            Assert.AreEqual(900, targetRow.EventId);
            Assert.AreEqual(TargetProfileId, targetRow.UserProfileId);
            CollectionAssert.Contains(_removedRows, placeholderRow);
        }

        [TestMethod]
        public async Task Conflict_TargetMoreAdvanced_KeepsTargetUnchanged()
        {
            var placeholderRow = PlaceholderRow(11, SkillStatus.InProgress, Earlier);
            var targetRow = TargetRow(11, SkillStatus.Completed, Later);

            await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(SkillStatus.Completed, targetRow.Status);
            Assert.AreEqual(Later, targetRow.UpdatedAtUtc);
            Assert.AreEqual(TargetMarker, targetRow.UpdatedById);
            Assert.AreEqual(800, targetRow.EventId);
            CollectionAssert.Contains(_removedRows, placeholderRow);
        }

        [TestMethod]
        public async Task Conflict_Tie_PlaceholderEarlier_Wins()
        {
            PlaceholderRow(11, SkillStatus.Completed, Earlier);
            var targetRow = TargetRow(11, SkillStatus.Completed, Later);

            await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(Earlier, targetRow.UpdatedAtUtc);
            Assert.AreEqual(PlaceholderMarker, targetRow.UpdatedById);
        }

        [TestMethod]
        public async Task Conflict_Tie_TargetEarlier_Wins()
        {
            PlaceholderRow(11, SkillStatus.InProgress, Later);
            var targetRow = TargetRow(11, SkillStatus.InProgress, Earlier);

            await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(Earlier, targetRow.UpdatedAtUtc);
            Assert.AreEqual(TargetMarker, targetRow.UpdatedById);
        }

        [TestMethod]
        public async Task Conflict_PreviewReportsTheSameWinnerAsMerge()
        {
            PlaceholderRow(11, SkillStatus.InProgress, Earlier);
            TargetRow(11, SkillStatus.Completed, Later);

            var preview = await _service.PreviewMergeAsync(PlaceholderId, TargetId);

            var conflict = preview.Data!.ProgressConflicts.Single();
            Assert.AreEqual(SkillStatus.InProgress, conflict.PlaceholderStatus);
            Assert.AreEqual(SkillStatus.Completed, conflict.TargetStatus);
            Assert.AreEqual(SkillStatus.Completed, conflict.KeptStatus);
        }

        // =====================================================================
        // Merge: moving data
        // =====================================================================

        [TestMethod]
        public async Task Merge_ProgressTheTargetLacks_IsRepointed_WithHistoryIntact()
        {
            var row = PlaceholderRow(10, SkillStatus.Completed, Earlier);

            await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(TargetProfileId, row.UserProfileId);
            Assert.AreEqual(SkillStatus.Completed, row.Status);
            Assert.AreEqual(Earlier, row.UpdatedAtUtc);
            Assert.AreEqual(PlaceholderMarker, row.UpdatedById);
            Assert.IsFalse(_removedRows.Contains(row));
        }

        [TestMethod]
        public async Task Merge_Enrollments_MoveOrDropDuplicates()
        {
            var moving = PlaceholderEnrollment(3);
            var duplicate = PlaceholderEnrollment(4);
            var targetsOwn = TargetEnrollment(4);

            await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(TargetProfileId, moving.UserProfileId);
            CollectionAssert.AreEqual(new[] { duplicate }, _removedEnrollments);
            Assert.AreEqual(TargetProfileId, targetsOwn.UserProfileId);
        }

        [TestMethod]
        public async Task Merge_OverlappingEvent_ReturnsConflict_AndWritesNothing()
        {
            var row = PlaceholderRow(10, SkillStatus.Completed);
            PlaceholderEnrollment(3);
            GivenOverlapFor(3);

            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            StringAssert.Contains(result.Error, "3");
            Assert.AreEqual(PlaceholderProfileId, row.UserProfileId, "Nothing should have been moved.");
            VerifyNothingWritten();
        }

        [TestMethod]
        public async Task Merge_DeletesThePlaceholder_NotTheTarget()
        {
            await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            _userManager.Verify(m => m.DeleteAsync(_placeholder), Times.Once);
            _userManager.Verify(m => m.DeleteAsync(_target), Times.Never);
        }

        [TestMethod]
        public async Task Merge_SavesMoves_BeforeDeleting_ThenCommits()
        {
            // Deleting first would cascade-delete the placeholder's progress and enrollments
            PlaceholderRow(10, SkillStatus.Completed);

            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.IsTrue(result.Succeeded);
            CollectionAssert.AreEqual(new[] { "Save", "Delete", "Commit" }, _callOrder);
            _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Merge_DeleteFails_ReturnsUnexpected_AndDoesNotCommit()
        {
            _userManager
                .Setup(m => m.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Boom" }));

            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.AreEqual(ResultErrorType.Unexpected, result.ErrorType);
            _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Merge_NothingToMove_StillDeletesThePlaceholder()
        {
            var result = await _service.MergeAsync(PlaceholderId, TargetId, AdminId);

            Assert.IsTrue(result.Succeeded);
            _userManager.Verify(m => m.DeleteAsync(_placeholder), Times.Once);
            _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // =====================================================================
        // Delete placeholder
        // =====================================================================

        [TestMethod]
        public async Task Delete_NoHistory_DeletesIt()
        {
            var result = await _service.DeletePlaceholderAsync(PlaceholderId, AdminId);

            Assert.IsTrue(result.Succeeded);
            _userManager.Verify(m => m.DeleteAsync(_placeholder), Times.Once);
        }

        [TestMethod]
        public async Task Delete_HasProgress_ReturnsConflict_AndDoesNotDelete()
        {
            PlaceholderRow(10, SkillStatus.InProgress);

            var result = await _service.DeletePlaceholderAsync(PlaceholderId, AdminId);

            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            _userManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [TestMethod]
        public async Task Delete_HasEnrollments_ReturnsConflict_AndDoesNotDelete()
        {
            PlaceholderEnrollment(3);

            var result = await _service.DeletePlaceholderAsync(PlaceholderId, AdminId);

            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            _userManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [TestMethod]
        public async Task Delete_FullAccount_ReturnsValidation_AndDoesNotDelete()
        {
            _placeholder.PasswordHash = "hashed";

            var result = await _service.DeletePlaceholderAsync(PlaceholderId, AdminId);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            _userManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [TestMethod]
        public async Task Delete_Missing_ReturnsNotFound()
        {
            _userManager.Setup(m => m.FindByIdAsync(PlaceholderId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.DeletePlaceholderAsync(PlaceholderId, AdminId);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }
    }
}