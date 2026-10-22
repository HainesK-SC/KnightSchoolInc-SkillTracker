using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class SkillProgressServiceTests
    {
        private const int SkillId = 10;
        private const int UserProfileId = 42;

        private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ActingUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid EarlierMarkerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        // The fake clock's "now", and an earlier time for rows that already exist
        private static readonly DateTimeOffset Now = new(2026, 10, 22, 15, 0, 0, TimeSpan.Zero);
        private static readonly DateTime EarlierUtc = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        private Mock<ISkillRepository> _skillRepository = null!;
        private Mock<IParticipantSkillProgressRepository> _progressRepository = null!;
        private Mock<IUserProfileRepository> _userProfileRepository = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private FakeTimeProvider _timeProvider = null!;
        private SkillProgressService _service = null!;

        private Skill _skill = null!;
        private ParticipantSkillProgress? _addedRow;

        [TestInitialize]
        public void TestInitialize()
        {
            _addedRow = null;

            // Default skill: exists and is active. Tests that need an archived skill flip IsArchived.
            _skill = new Skill
            {
                SkillId = SkillId,
                Name = "Parry",
                Description = "Deflect an incoming strike.",
                IsArchived = false,
                DisciplineId = 1
            };

            _skillRepository = new Mock<ISkillRepository>();
            _skillRepository
                .Setup(r => r.GetSkillByIdAsync(SkillId))
                .ReturnsAsync(() => _skill);

            // Default participant: has a profile
            _userProfileRepository = new Mock<IUserProfileRepository>();
            _userProfileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(UserId))
                .ReturnsAsync(new UserProfile { UserProfileId = UserProfileId, ApplicationUserId = UserId });

            // Default progress: no row yet (Locked). Remember whatever row gets added.
            _progressRepository = new Mock<IParticipantSkillProgressRepository>();
            _progressRepository
                .Setup(r => r.GetForUpdateAsync(UserProfileId, SkillId))
                .ReturnsAsync((ParticipantSkillProgress?)null);
            _progressRepository
                .Setup(r => r.AddNewProgressRow(It.IsAny<ParticipantSkillProgress>()))
                .Callback<ParticipantSkillProgress>(row => _addedRow = row);

            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _timeProvider = new FakeTimeProvider(Now);

            _service = new SkillProgressService(
                _skillRepository.Object,
                _progressRepository.Object,
                _userProfileRepository.Object,
                _unitOfWork.Object,
                _timeProvider,
                NullLogger<SkillProgressService>.Instance);
        }

        // Makes the repository return an existing row for this participant + skill
        private ParticipantSkillProgress GivenExistingRow(SkillStatus status)
        {
            var row = new ParticipantSkillProgress
            {
                ParticipantSkillProgressId = 7,
                UserProfileId = UserProfileId,
                SkillId = SkillId,
                Status = status,
                UpdatedAtUtc = EarlierUtc,
                UpdatedById = EarlierMarkerId
            };

            _progressRepository
                .Setup(r => r.GetForUpdateAsync(UserProfileId, SkillId))
                .ReturnsAsync(row);

            return row;
        }

        private Task<Result<Capstone.Identity.API.Dtos.Response.ParticipantSkillProgressDto>> SetStatus(SkillStatus status)
        {
            return _service.SetSkillStatusAsync(UserId, SkillId, status, ActingUserId);
        }

        private void VerifyNoWrites()
        {
            _progressRepository.Verify(r => r.AddNewProgressRow(It.IsAny<ParticipantSkillProgress>()), Times.Never);
            _progressRepository.Verify(r => r.RemoveProgressRow(It.IsAny<ParticipantSkillProgress>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // ---------- Guard clauses ----------

        [TestMethod]
        public async Task SetStatus_UndefinedStatus_ReturnsValidation_AndTouchesNothing()
        {
            var result = await SetStatus((SkillStatus)99);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            _skillRepository.Verify(r => r.GetSkillByIdAsync(It.IsAny<int>()), Times.Never);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task SetStatus_SkillNotFound_ReturnsNotFound_AndDoesNotWrite()
        {
            _skillRepository
                .Setup(r => r.GetSkillByIdAsync(SkillId))
                .ReturnsAsync((Skill?)null);

            var result = await SetStatus(SkillStatus.InProgress);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task SetStatus_ProfileNotFound_ReturnsNotFound_AndDoesNotWrite()
        {
            _userProfileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(UserId))
                .ReturnsAsync((UserProfile?)null);

            var result = await SetStatus(SkillStatus.InProgress);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            _progressRepository.Verify(r => r.GetForUpdateAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task SetStatus_LooksUpProgressByProfileId_NotUserGuid()
        {
            await SetStatus(SkillStatus.InProgress);

            _progressRepository.Verify(r => r.GetForUpdateAsync(UserProfileId, SkillId), Times.Once);
        }

        // ---------- Locked (undo) ----------

        [TestMethod]
        public async Task Locked_NoExistingRow_ReturnsLocked_AndDoesNotWrite()
        {
            var result = await SetStatus(SkillStatus.Locked);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(SkillStatus.Locked, result.Data!.Status);
            Assert.IsNull(result.Data.UpdatedAtUtc);
            Assert.IsNull(result.Data.UpdatedById);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Locked_ExistingRow_RemovesRow_AndSavesOnce()
        {
            var row = GivenExistingRow(SkillStatus.Completed);

            var result = await SetStatus(SkillStatus.Locked);

            Assert.IsTrue(result.Succeeded);
            _progressRepository.Verify(r => r.RemoveProgressRow(row), Times.Once);
            _progressRepository.Verify(r => r.AddNewProgressRow(It.IsAny<ParticipantSkillProgress>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Locked_ExistingRow_ReturnsLockedDto_WithNullTimeAndMarker()
        {
            GivenExistingRow(SkillStatus.Completed);

            var result = await SetStatus(SkillStatus.Locked);

            var dto = result.Data!;
            Assert.AreEqual(UserId, dto.UserId);
            Assert.AreEqual(SkillId, dto.SkillId);
            Assert.AreEqual(SkillStatus.Locked, dto.Status);
            Assert.IsNull(dto.UpdatedAtUtc);
            Assert.IsNull(dto.UpdatedById);
        }

        [TestMethod]
        public async Task Locked_OnArchivedSkill_IsStillAllowed()
        {
            _skill.IsArchived = true;
            var row = GivenExistingRow(SkillStatus.InProgress);

            var result = await SetStatus(SkillStatus.Locked);

            Assert.IsTrue(result.Succeeded);
            _progressRepository.Verify(r => r.RemoveProgressRow(row), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ---------- Archived skills ----------

        [TestMethod]
        [DataRow(SkillStatus.InProgress)]
        [DataRow(SkillStatus.Completed)]
        public async Task RecordProgress_OnArchivedSkill_ReturnsValidation_AndDoesNotWrite(SkillStatus status)
        {
            _skill.IsArchived = true;

            var result = await SetStatus(status);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task RecordProgress_OnArchivedSkill_WithExistingRow_LeavesRowUnchanged()
        {
            _skill.IsArchived = true;
            var row = GivenExistingRow(SkillStatus.InProgress);

            await SetStatus(SkillStatus.Completed);

            Assert.AreEqual(SkillStatus.InProgress, row.Status);
            Assert.AreEqual(EarlierUtc, row.UpdatedAtUtc);
            Assert.AreEqual(EarlierMarkerId, row.UpdatedById);
            VerifyNoWrites();
        }

        // ---------- No row yet: create ----------

        [TestMethod]
        [DataRow(SkillStatus.InProgress)]
        [DataRow(SkillStatus.Completed)]
        public async Task NoExistingRow_AddsRowWithStatusTimeAndMarker(SkillStatus status)
        {
            var result = await SetStatus(status);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_addedRow);
            Assert.AreEqual(UserProfileId, _addedRow.UserProfileId);
            Assert.AreEqual(SkillId, _addedRow.SkillId);
            Assert.AreEqual(status, _addedRow.Status);
            Assert.AreEqual(Now.UtcDateTime, _addedRow.UpdatedAtUtc);
            Assert.AreEqual(ActingUserId, _addedRow.UpdatedById);
        }

        [TestMethod]
        public async Task NoExistingRow_SavesOnce_AndRemovesNothing()
        {
            await SetStatus(SkillStatus.InProgress);

            _progressRepository.Verify(r => r.AddNewProgressRow(It.IsAny<ParticipantSkillProgress>()), Times.Once);
            _progressRepository.Verify(r => r.RemoveProgressRow(It.IsAny<ParticipantSkillProgress>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task NoExistingRow_ReturnsDto_WithUserGuidAndNewValues()
        {
            var result = await SetStatus(SkillStatus.Completed);

            var dto = result.Data!;
            Assert.AreEqual(UserId, dto.UserId);
            Assert.AreEqual(SkillId, dto.SkillId);
            Assert.AreEqual(SkillStatus.Completed, dto.Status);
            Assert.AreEqual(Now.UtcDateTime, dto.UpdatedAtUtc);
            Assert.AreEqual(ActingUserId, dto.UpdatedById);
        }

        // ---------- Existing row, same status: no-op ----------

        [TestMethod]
        [DataRow(SkillStatus.InProgress)]
        [DataRow(SkillStatus.Completed)]
        public async Task SameStatus_DoesNotWrite_AndKeepsOriginalTimeAndMarker(SkillStatus status)
        {
            var row = GivenExistingRow(status);

            var result = await SetStatus(status);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(EarlierUtc, row.UpdatedAtUtc);
            Assert.AreEqual(EarlierMarkerId, row.UpdatedById);
            Assert.AreEqual(EarlierUtc, result.Data!.UpdatedAtUtc);
            Assert.AreEqual(EarlierMarkerId, result.Data.UpdatedById);
            VerifyNoWrites();
        }

        // ---------- Existing row, different status: update ----------

        [TestMethod]
        [DataRow(SkillStatus.InProgress, SkillStatus.Completed)]
        [DataRow(SkillStatus.Completed, SkillStatus.InProgress)]
        public async Task DifferentStatus_UpdatesStatusTimeAndMarker_AndSavesOnce(SkillStatus from, SkillStatus to)
        {
            var row = GivenExistingRow(from);

            var result = await SetStatus(to);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(to, row.Status);
            Assert.AreEqual(Now.UtcDateTime, row.UpdatedAtUtc);
            Assert.AreEqual(ActingUserId, row.UpdatedById);

            // The tracked row is changed in place: no add, no remove, one save
            _progressRepository.Verify(r => r.AddNewProgressRow(It.IsAny<ParticipantSkillProgress>()), Times.Never);
            _progressRepository.Verify(r => r.RemoveProgressRow(It.IsAny<ParticipantSkillProgress>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DifferentStatus_ReturnsDto_WithUserGuidAndUpdatedValues()
        {
            GivenExistingRow(SkillStatus.InProgress);

            var result = await SetStatus(SkillStatus.Completed);

            var dto = result.Data!;
            Assert.AreEqual(UserId, dto.UserId);
            Assert.AreEqual(SkillStatus.Completed, dto.Status);
            Assert.AreEqual(Now.UtcDateTime, dto.UpdatedAtUtc);
            Assert.AreEqual(ActingUserId, dto.UpdatedById);
        }
    }
}