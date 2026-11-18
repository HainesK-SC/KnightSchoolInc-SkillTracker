using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
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
    public class EventSkillProgressServiceTests
    {
        private const int EventId = 5;
        private const int OtherEventId = 6;
        private const int ProfileId = 42;

        // Skills on the event
        private const int ParryId = 10;
        private const int LungeId = 11;
        private const int LongbowId = 20;
        // A skill that exists but is NOT on the event
        private const int NotOnEventId = 99;

        private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid InstructorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid AdminId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid EarlierMarkerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private static readonly DateTimeOffset Now = new(2026, 12, 5, 16, 0, 0, TimeSpan.Zero);
        private static readonly DateTime EarlierUtc = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        private Mock<IEventRepository> _eventRepository = null!;
        private Mock<IEventStaffRepository> _staffRepository = null!;
        private Mock<IEventEnrollmentRepository> _enrollmentRepository = null!;
        private Mock<IEventSkillRepository> _eventSkillRepository = null!;
        private Mock<IDisciplineRepository> _disciplineRepository = null!;
        private Mock<IParticipantSkillProgressRepository> _progressRepository = null!;
        private Mock<IUserProfileRepository> _profileRepository = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private FakeTimeProvider _timeProvider = null!;
        private EventSkillProgressService _service = null!;

        private Event _event = null!;
        private List<ParticipantSkillProgress> _rows = [];        // the participant's progress rows
        private List<ParticipantSkillProgress> _addedRows = [];
        private List<ParticipantSkillProgress> _removedRows = [];
        private List<string> _callOrder = [];

        [TestInitialize]
        public void TestInitialize()
        {
            _rows = [];
            _addedRows = [];
            _removedRows = [];
            _callOrder = [];

            _event = new Event
            {
                EventId = EventId,
                EventName = "Spring Tournament",
                EventLocation = "Toronto",
                EventStart = new DateTime(2026, 12, 5, 15, 0, 0, DateTimeKind.Utc),
                EventEnd = new DateTime(2026, 12, 5, 22, 0, 0, DateTimeKind.Utc),
                IsCancelled = false
            };
            _eventRepository = new Mock<IEventRepository>();
            _eventRepository.Setup(r => r.GetEventByIdAsync(EventId)).ReturnsAsync(() => _event);

            // Default: the instructor is assigned to the event
            _staffRepository = new Mock<IEventStaffRepository>();
            _staffRepository.Setup(r => r.IsAssignedAsync(EventId, InstructorId)).ReturnsAsync(true);

            // Default: the participant has a profile and is enrolled
            _profileRepository = new Mock<IUserProfileRepository>();
            _profileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(UserId))
                .ReturnsAsync(new UserProfile { UserProfileId = ProfileId, ApplicationUserId = UserId });

            _enrollmentRepository = new Mock<IEventEnrollmentRepository>();
            _enrollmentRepository.Setup(r => r.IsEnrolledAsync(EventId, ProfileId)).ReturnsAsync(true);

            // The event's skills: Parry and Lunge (Swordplay), Longbow (Archery)
            _eventSkillRepository = new Mock<IEventSkillRepository>();
            _eventSkillRepository
                .Setup(r => r.GetActiveSkillsForEventAsync(EventId))
                .ReturnsAsync(new List<Skill>
                {
                    NewSkill(ParryId, 1, "Parry"),
                    NewSkill(LungeId, 1, "Lunge"),
                    NewSkill(LongbowId, 2, "Longbow")
                });

            _disciplineRepository = new Mock<IDisciplineRepository>();
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(new List<Discipline>
                {
                    new() { DisciplineId = 1, Name = "Swordplay" },
                    new() { DisciplineId = 2, Name = "Archery" },
                    new() { DisciplineId = 3, Name = "Horsemanship" }   // none of its skills are on the event
                });

            // Progress: both reads return whatever _rows holds at call time
            _progressRepository = new Mock<IParticipantSkillProgressRepository>();
            _progressRepository
                .Setup(r => r.GetForUserForUpdateAsync(ProfileId))
                .ReturnsAsync(() => _rows);
            _progressRepository
                .Setup(r => r.GetForUserAsync(ProfileId))
                .Callback(() => _callOrder.Add("ReadView"))
                .ReturnsAsync(() => _rows);
            _progressRepository
                .Setup(r => r.AddNewProgressRow(It.IsAny<ParticipantSkillProgress>()))
                .Callback<ParticipantSkillProgress>(row => _addedRows.Add(row));
            _progressRepository
                .Setup(r => r.RemoveProgressRow(It.IsAny<ParticipantSkillProgress>()))
                .Callback<ParticipantSkillProgress>(row => _removedRows.Add(row));

            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork
                .Setup(u => u.SaveChangesAsync())
                .Callback(() => _callOrder.Add("Save"))
                .ReturnsAsync(1);

            _timeProvider = new FakeTimeProvider(Now);

            _service = new EventSkillProgressService(
                _eventRepository.Object,
                _staffRepository.Object,
                _enrollmentRepository.Object,
                _eventSkillRepository.Object,
                _disciplineRepository.Object,
                _progressRepository.Object,
                _profileRepository.Object,
                _unitOfWork.Object,
                _timeProvider,
                NullLogger<EventSkillProgressService>.Instance);
        }

        // ---------- Helpers ----------

        private static Skill NewSkill(int id, int disciplineId, string name)
        {
            return new Skill
            {
                SkillId = id,
                DisciplineId = disciplineId,
                Name = name,
                Description = $"{name} description",
                IsArchived = false
            };
        }

        private ParticipantSkillProgress GivenRow(int skillId, SkillStatus status, int? eventId = OtherEventId)
        {
            var row = new ParticipantSkillProgress
            {
                UserProfileId = ProfileId,
                SkillId = skillId,
                Status = status,
                UpdatedAtUtc = EarlierUtc,
                UpdatedById = EarlierMarkerId,
                EventId = eventId
            };
            _rows.Add(row);
            return row;
        }

        private static SetEventSkillStatusesRequestDto Changes(params (int skillId, SkillStatus status)[] changes)
        {
            return new SetEventSkillStatusesRequestDto
            {
                Changes = changes
                    .Select(c => new SkillStatusChangeDto { SkillId = c.skillId, Status = c.status })
                    .ToList()
            };
        }

        private Task<Result<SkillTreeDto>> Save(params (int skillId, SkillStatus status)[] changes)
        {
            return _service.SetEventSkillStatusesAsync(EventId, UserId, Changes(changes), InstructorId, callerIsAdmin: false);
        }

        private static SkillTreeSkillDto FindSkill(SkillTreeDto tree, int skillId)
        {
            return tree.Disciplines.SelectMany(d => d.Skills).Single(s => s.SkillId == skillId);
        }

        private void VerifyNoWrites()
        {
            Assert.AreEqual(0, _addedRows.Count, "Nothing should be added.");
            Assert.AreEqual(0, _removedRows.Count, "Nothing should be removed.");
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // =====================================================================
        // Read
        // =====================================================================

        [TestMethod]
        public async Task Read_EventMissing_ReturnsNotFound()
        {
            _eventRepository.Setup(r => r.GetEventByIdAsync(EventId)).ReturnsAsync((Event?)null);

            var result = await _service.GetParticipantEventSkillsAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task Read_UnassignedInstructor_ReturnsNotFound()
        {
            _staffRepository.Setup(r => r.IsAssignedAsync(EventId, InstructorId)).ReturnsAsync(false);

            var result = await _service.GetParticipantEventSkillsAsync(EventId, UserId, InstructorId, callerIsAdmin: false);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task Read_Admin_SkipsAssignmentCheck()
        {
            var result = await _service.GetParticipantEventSkillsAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            _staffRepository.Verify(r => r.IsAssignedAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task Read_ParticipantWithoutProfile_ReturnsNotFound()
        {
            _profileRepository.Setup(r => r.GetUserProfileByUserIdAsync(UserId)).ReturnsAsync((UserProfile?)null);

            var result = await _service.GetParticipantEventSkillsAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task Read_ParticipantNotEnrolled_ReturnsNotFound()
        {
            _enrollmentRepository.Setup(r => r.IsEnrolledAsync(EventId, ProfileId)).ReturnsAsync(false);

            var result = await _service.GetParticipantEventSkillsAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task Read_ShowsOnlyTheEventsSkills_WithStatuses()
        {
            GivenRow(ParryId, SkillStatus.Completed);
            GivenRow(NotOnEventId, SkillStatus.InProgress);   // progress on a skill that isn't on this event

            var result = await _service.GetParticipantEventSkillsAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            var tree = result.Data!;
            var allSkillIds = tree.Disciplines.SelectMany(d => d.Skills).Select(s => s.SkillId).ToList();
            CollectionAssert.AreEquivalent(new[] { ParryId, LungeId, LongbowId }, allSkillIds);

            Assert.AreEqual(SkillStatus.Completed, FindSkill(tree, ParryId).Status);
            Assert.AreEqual(SkillStatus.Locked, FindSkill(tree, LungeId).Status);
            Assert.IsNull(FindSkill(tree, LungeId).UpdatedAtUtc);
        }

        [TestMethod]
        public async Task Read_GroupsAndSorts_AndOmitsDisciplinesWithNoEventSkills()
        {
            var result = await _service.GetParticipantEventSkillsAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            var tree = result.Data!;
            Assert.AreEqual(UserId, tree.UserId);
            CollectionAssert.AreEqual(new[] { "Archery", "Swordplay" }, tree.Disciplines.Select(d => d.Name).ToList());
            CollectionAssert.AreEqual(new[] { "Lunge", "Parry" }, tree.Disciplines[1].Skills.Select(s => s.Name).ToList());
        }

        // =====================================================================
        // Save: guards (nothing is written)
        // =====================================================================

        [TestMethod]
        public async Task Save_UnassignedInstructor_ReturnsNotFound_AndDoesNotWrite()
        {
            _staffRepository.Setup(r => r.IsAssignedAsync(EventId, InstructorId)).ReturnsAsync(false);

            var result = await Save((ParryId, SkillStatus.Completed));

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_CancelledEvent_ReturnsValidation_AndDoesNotWrite()
        {
            _event.IsCancelled = true;

            var result = await Save((ParryId, SkillStatus.Completed));

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_ParticipantNotEnrolled_ReturnsNotFound_AndDoesNotWrite()
        {
            _enrollmentRepository.Setup(r => r.IsEnrolledAsync(EventId, ProfileId)).ReturnsAsync(false);

            var result = await Save((ParryId, SkillStatus.Completed));

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_DuplicateSkillInRequest_ReturnsValidation_AndDoesNotWrite()
        {
            var result = await Save((ParryId, SkillStatus.InProgress), (ParryId, SkillStatus.Completed));

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            StringAssert.Contains(result.Error, ParryId.ToString());
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_UndefinedStatus_ReturnsValidation_AndDoesNotWrite()
        {
            var result = await Save((ParryId, (SkillStatus)99));

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_SkillNotOnEvent_ReturnsValidation_NamingIt_AndDoesNotWrite()
        {
            var result = await Save((ParryId, SkillStatus.Completed), (NotOnEventId, SkillStatus.Completed));

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            StringAssert.Contains(result.Error, NotOnEventId.ToString());
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_OneInvalidChange_AppliesNoneOfTheValidOnes()
        {
            // All-or-nothing: the valid Parry change must not be applied either
            var parryRow = GivenRow(ParryId, SkillStatus.InProgress);

            await Save((ParryId, SkillStatus.Completed), (NotOnEventId, SkillStatus.Completed));

            Assert.AreEqual(SkillStatus.InProgress, parryRow.Status);
            Assert.AreEqual(EarlierUtc, parryRow.UpdatedAtUtc);
            VerifyNoWrites();
        }

        // =====================================================================
        // Save: per-skill rules
        // =====================================================================

        [TestMethod]
        public async Task Save_NoRow_CreatesOne_WithEventTimeAndMarker()
        {
            var result = await Save((ParryId, SkillStatus.InProgress));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, _addedRows.Count);
            var added = _addedRows[0];
            Assert.AreEqual(ProfileId, added.UserProfileId);
            Assert.AreEqual(ParryId, added.SkillId);
            Assert.AreEqual(SkillStatus.InProgress, added.Status);
            Assert.AreEqual(Now.UtcDateTime, added.UpdatedAtUtc);
            Assert.AreEqual(InstructorId, added.UpdatedById);
            Assert.AreEqual(EventId, added.EventId);
        }

        [TestMethod]
        public async Task Save_LockedWithRow_RemovesIt()
        {
            var row = GivenRow(ParryId, SkillStatus.Completed);

            await Save((ParryId, SkillStatus.Locked));

            CollectionAssert.AreEqual(new[] { row }, _removedRows);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Save_LockedWithNoRow_DoesNothing_AndDoesNotSave()
        {
            var result = await Save((ParryId, SkillStatus.Locked));

            Assert.IsTrue(result.Succeeded);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_SameStatus_LeavesRowAlone_AndDoesNotSave()
        {
            var row = GivenRow(ParryId, SkillStatus.InProgress);

            var result = await Save((ParryId, SkillStatus.InProgress));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(EarlierUtc, row.UpdatedAtUtc);
            Assert.AreEqual(EarlierMarkerId, row.UpdatedById);
            Assert.AreEqual(OtherEventId, row.EventId);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_DifferentStatus_UpdatesRow_AndRecordsThisEvent()
        {
            // The row was last set at another event; changing it here moves the event to this one
            var row = GivenRow(ParryId, SkillStatus.InProgress, eventId: OtherEventId);

            await Save((ParryId, SkillStatus.Completed));

            Assert.AreEqual(SkillStatus.Completed, row.Status);
            Assert.AreEqual(Now.UtcDateTime, row.UpdatedAtUtc);
            Assert.AreEqual(InstructorId, row.UpdatedById);
            Assert.AreEqual(EventId, row.EventId);
            Assert.AreEqual(0, _addedRows.Count);
            Assert.AreEqual(0, _removedRows.Count);
        }

        [TestMethod]
        public async Task Save_MixedBatch_AppliesEachRule_WithOneSave()
        {
            var lungeRow = GivenRow(LungeId, SkillStatus.InProgress);
            var longbowRow = GivenRow(LongbowId, SkillStatus.Completed);

            await Save(
                (ParryId, SkillStatus.InProgress),     // create
                (LungeId, SkillStatus.Completed),      // update
                (LongbowId, SkillStatus.Locked));      // remove

            Assert.AreEqual(ParryId, _addedRows.Single().SkillId);
            Assert.AreEqual(SkillStatus.Completed, lungeRow.Status);
            CollectionAssert.AreEqual(new[] { longbowRow }, _removedRows);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Save_EmptyChanges_ReturnsView_AndDoesNotSave()
        {
            var result = await _service.SetEventSkillStatusesAsync(
                EventId, UserId, new SetEventSkillStatusesRequestDto { Changes = [] }, InstructorId, callerIsAdmin: false);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(2, result.Data!.Disciplines.Count);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Save_Admin_SkipsAssignmentCheck()
        {
            var result = await _service.SetEventSkillStatusesAsync(
                EventId, UserId, Changes((ParryId, SkillStatus.Completed)), AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(AdminId, _addedRows.Single().UpdatedById);
            _staffRepository.Verify(r => r.IsAssignedAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
        }

        // =====================================================================
        // Save: response
        // =====================================================================

        [TestMethod]
        public async Task Save_ReturnsTheView_ReadAfterTheSave()
        {
            await Save((ParryId, SkillStatus.Completed));

            var saveIndex = _callOrder.IndexOf("Save");
            var readIndex = _callOrder.LastIndexOf("ReadView");
            Assert.IsTrue(saveIndex >= 0, "Expected a save.");
            Assert.IsTrue(readIndex > saveIndex,
                $"The view must be read after the save. Call order: {string.Join(", ", _callOrder)}");
        }

        [TestMethod]
        public async Task Save_ReturnsTheEventsSkillsTree()
        {
            GivenRow(LungeId, SkillStatus.Completed);

            var result = await Save((ParryId, SkillStatus.InProgress));

            var tree = result.Data!;
            Assert.AreEqual(UserId, tree.UserId);
            CollectionAssert.AreEqual(new[] { "Archery", "Swordplay" }, tree.Disciplines.Select(d => d.Name).ToList());
            Assert.AreEqual(SkillStatus.Completed, FindSkill(tree, LungeId).Status);
        }
    }
}