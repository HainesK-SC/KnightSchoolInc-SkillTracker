using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class EventSkillServiceTests
    {
        private const int EventId = 5;
        private static readonly Guid AdminId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid InstructorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private Mock<IEventRepository> _eventRepository = null!;
        private Mock<IEventSkillRepository> _eventSkillRepository = null!;
        private Mock<ISkillRepository> _skillRepository = null!;
        private Mock<IDisciplineRepository> _disciplineRepository = null!;
        private Mock<IEventStaffRepository> _staffRepository = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IDbContextTransaction> _transaction = null!;
        private EventSkillService _service = null!;

        // What the mocked repositories hold / return
        private List<Skill> _knownSkills = [];          // every skill that "exists" (for GetSkillsByIdsAsync)
        private List<EventSkill> _currentRows = [];     // the event's current EventSkill rows
        private List<Skill> _eventActiveSkills = [];    // what GetActiveSkillsForEventAsync returns
        private List<Discipline> _disciplines = [];

        // What the service staged
        private List<EventSkill>? _removedRows;
        private List<EventSkill>? _addedRows;
        private List<string> _callOrder = [];

        [TestInitialize]
        public void TestInitialize()
        {
            _knownSkills = [];
            _currentRows = [];
            _eventActiveSkills = [];
            _disciplines = [];
            _removedRows = null;
            _addedRows = null;
            _callOrder = [];

            // Default: the event exists
            _eventRepository = new Mock<IEventRepository>();
            _eventRepository
                .Setup(r => r.GetEventByIdAsync(EventId))
                .ReturnsAsync(new Event { EventId = EventId, EventName = "Spring Tournament", EventLocation = "Toronto" });

            _skillRepository = new Mock<ISkillRepository>();
            _skillRepository
                .Setup(r => r.GetSkillsByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
                .ReturnsAsync((IReadOnlyCollection<int> ids) => _knownSkills.Where(s => ids.Contains(s.SkillId)).ToList());

            _eventSkillRepository = new Mock<IEventSkillRepository>();
            _eventSkillRepository
                .Setup(r => r.GetEventSkillsForUpdateAsync(EventId))
                .ReturnsAsync(() => _currentRows);
            _eventSkillRepository
                .Setup(r => r.GetActiveSkillsForEventAsync(EventId))
                .ReturnsAsync(() => _eventActiveSkills);
            _eventSkillRepository
                .Setup(r => r.RemoveEventSkills(It.IsAny<IEnumerable<EventSkill>>()))
                .Callback<IEnumerable<EventSkill>>(rows =>
                {
                    _removedRows = rows.ToList();
                    _callOrder.Add("Remove");
                });
            _eventSkillRepository
                .Setup(r => r.AddEventSkills(It.IsAny<IEnumerable<EventSkill>>()))
                .Callback<IEnumerable<EventSkill>>(rows =>
                {
                    _addedRows = rows.ToList();
                    _callOrder.Add("Add");
                });

            _disciplineRepository = new Mock<IDisciplineRepository>();
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(() => _disciplines);

            // Default: the instructor is assigned to the event
            _staffRepository = new Mock<IEventStaffRepository>();
            _staffRepository
                .Setup(r => r.IsAssignedAsync(EventId, InstructorId))
                .ReturnsAsync(true);

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

            _service = new EventSkillService(
                _eventRepository.Object,
                _eventSkillRepository.Object,
                _skillRepository.Object,
                _disciplineRepository.Object,
                _staffRepository.Object,
                _unitOfWork.Object,
                NullLogger<EventSkillService>.Instance);
        }

        // ---------- Helpers ----------

        private Skill KnownSkill(int id, int disciplineId, string name, bool isArchived = false)
        {
            var skill = new Skill
            {
                SkillId = id,
                DisciplineId = disciplineId,
                Name = name,
                Description = $"{name} description",
                IsArchived = isArchived
            };
            _knownSkills.Add(skill);
            return skill;
        }

        private void GivenCurrentRows(params int[] skillIds)
        {
            var nextId = 100;
            foreach (var skillId in skillIds)
            {
                _currentRows.Add(new EventSkill { Id = nextId, EventId = EventId, SkillId = skillId });
                nextId++;
            }
        }

        private static SetEventSkillsRequestDto Request(params int[] skillIds)
        {
            return new SetEventSkillsRequestDto { SkillIds = skillIds.ToList() };
        }

        private void GivenEventMissing()
        {
            _eventRepository.Setup(r => r.GetEventByIdAsync(EventId)).ReturnsAsync((Event?)null);
        }

        private void VerifyNoWrites()
        {
            _eventSkillRepository.Verify(r => r.RemoveEventSkills(It.IsAny<IEnumerable<EventSkill>>()), Times.Never);
            _eventSkillRepository.Verify(r => r.AddEventSkills(It.IsAny<IEnumerable<EventSkill>>()), Times.Never);
            _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // =====================================================================
        // Set: guards
        // =====================================================================

        [TestMethod]
        public async Task Set_EventMissing_ReturnsNotFound_AndDoesNotWrite()
        {
            GivenEventMissing();
            KnownSkill(10, 1, "Parry");

            var result = await _service.SetEventSkillsAsync(EventId, Request(10), AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Set_UnknownSkillId_ReturnsValidation_NamingTheId_AndDoesNotWrite()
        {
            KnownSkill(10, 1, "Parry");

            var result = await _service.SetEventSkillsAsync(EventId, Request(10, 999), AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            StringAssert.Contains(result.Error, "999");
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Set_ArchivedSkill_ReturnsValidation_NamingTheId_AndDoesNotWrite()
        {
            KnownSkill(10, 1, "Parry");
            KnownSkill(11, 1, "Lunge", isArchived: true);

            var result = await _service.SetEventSkillsAsync(EventId, Request(10, 11), AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            StringAssert.Contains(result.Error, "11");
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Set_InvalidRequest_LeavesExistingRowsUntouched()
        {
            GivenCurrentRows(10);
            KnownSkill(10, 1, "Parry");

            await _service.SetEventSkillsAsync(EventId, Request(10, 999), AdminId);

            Assert.IsNull(_removedRows, "Nothing should be removed when the request is invalid.");
        }

        // =====================================================================
        // Set: wipe and re-add
        // =====================================================================

        [TestMethod]
        public async Task Set_RemovesAllCurrentRows()
        {
            GivenCurrentRows(10, 11);
            KnownSkill(10, 1, "Parry");
            KnownSkill(11, 1, "Lunge");
            KnownSkill(20, 2, "Longbow");

            await _service.SetEventSkillsAsync(EventId, Request(11, 20), AdminId);

            Assert.IsNotNull(_removedRows);
            CollectionAssert.AreEquivalent(_currentRows, _removedRows);
        }

        [TestMethod]
        public async Task Set_AddsOneRowPerRequestedSkill_ForThisEvent()
        {
            KnownSkill(11, 1, "Lunge");
            KnownSkill(20, 2, "Longbow");

            await _service.SetEventSkillsAsync(EventId, Request(11, 20), AdminId);

            Assert.IsNotNull(_addedRows);
            CollectionAssert.AreEquivalent(new[] { 11, 20 }, _addedRows.Select(r => r.SkillId).ToList());
            Assert.IsTrue(_addedRows.All(r => r.EventId == EventId));
        }

        [TestMethod]
        public async Task Set_DuplicateIdsInRequest_AddEachSkillOnce()
        {
            KnownSkill(10, 1, "Parry");

            await _service.SetEventSkillsAsync(EventId, Request(10, 10, 10), AdminId);

            Assert.IsNotNull(_addedRows);
            Assert.AreEqual(1, _addedRows.Count);
        }

        [TestMethod]
        public async Task Set_RemovesAndSaves_BeforeAdding_ThenCommits()
        {
            // Order matters: old rows must be gone before the same (EventId, SkillId)
            // pairs are inserted again, or the unique index would reject them
            GivenCurrentRows(10);
            KnownSkill(10, 1, "Parry");

            await _service.SetEventSkillsAsync(EventId, Request(10), AdminId);

            CollectionAssert.AreEqual(
                new[] { "Remove", "Save", "Add", "Save", "Commit" },
                _callOrder);
        }

        [TestMethod]
        public async Task Set_UsesOneTransaction_AndCommitsOnce()
        {
            KnownSkill(10, 1, "Parry");

            await _service.SetEventSkillsAsync(EventId, Request(10), AdminId);

            _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Set_SecondSaveFails_DoesNotCommit()
        {
            // If adding the new rows fails, the removal must not be committed on its own
            GivenCurrentRows(10);
            KnownSkill(10, 1, "Parry");

            var saveCount = 0;
            _unitOfWork
                .Setup(u => u.SaveChangesAsync())
                .Returns(() =>
                {
                    saveCount++;
                    if (saveCount == 2)
                    {
                        throw new InvalidOperationException("Simulated database failure");
                    }
                    return Task.FromResult(1);
                });

            var threw = false;
            try
            {
                await _service.SetEventSkillsAsync(EventId, Request(10), AdminId);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            Assert.IsTrue(threw, "The failure should propagate to the global exception handler.");
            _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Set_EmptyList_ClearsTheEvent()
        {
            GivenCurrentRows(10, 11);

            var result = await _service.SetEventSkillsAsync(EventId, Request(), AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_removedRows);
            Assert.AreEqual(2, _removedRows.Count);
            Assert.IsNotNull(_addedRows);
            Assert.AreEqual(0, _addedRows.Count);
            _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Set_SameListAsCurrent_Succeeds()
        {
            GivenCurrentRows(10, 11);
            KnownSkill(10, 1, "Parry");
            KnownSkill(11, 1, "Lunge");

            var result = await _service.SetEventSkillsAsync(EventId, Request(10, 11), AdminId);

            Assert.IsTrue(result.Succeeded);
            CollectionAssert.AreEquivalent(new[] { 10, 11 }, _addedRows!.Select(r => r.SkillId).ToList());
        }

        [TestMethod]
        public async Task Set_ReturnsTheUpdatedGroupedList()
        {
            KnownSkill(10, 1, "Parry");
            _disciplines = [new Discipline { DisciplineId = 1, Name = "Swordplay" }];
            _eventActiveSkills = [_knownSkills[0]];

            var result = await _service.SetEventSkillsAsync(EventId, Request(10), AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, result.Data!.Count);
            Assert.AreEqual("Swordplay", result.Data[0].Name);
            Assert.AreEqual(10, result.Data[0].Skills.Single().SkillId);
        }

        [TestMethod]
        public async Task Set_DoesNotCheckStaffAssignment()
        {
            // Set is admin-only; the result is read back as an admin, so no assignment check
            KnownSkill(10, 1, "Parry");

            await _service.SetEventSkillsAsync(EventId, Request(10), AdminId);

            _staffRepository.Verify(r => r.IsAssignedAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
        }

        // =====================================================================
        // Get: access
        // =====================================================================

        [TestMethod]
        public async Task Get_EventMissing_ReturnsNotFound()
        {
            GivenEventMissing();

            var result = await _service.GetEventSkillsAsync(EventId, AdminId, callerIsAdmin: true);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task Get_Admin_DoesNotNeedAnAssignment()
        {
            var result = await _service.GetEventSkillsAsync(EventId, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            _staffRepository.Verify(r => r.IsAssignedAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task Get_AssignedInstructor_Succeeds()
        {
            var result = await _service.GetEventSkillsAsync(EventId, InstructorId, callerIsAdmin: false);

            Assert.IsTrue(result.Succeeded);
            _staffRepository.Verify(r => r.IsAssignedAsync(EventId, InstructorId), Times.Once);
        }

        [TestMethod]
        public async Task Get_UnassignedInstructor_ReturnsNotFound_AndLoadsNoSkills()
        {
            _staffRepository.Setup(r => r.IsAssignedAsync(EventId, InstructorId)).ReturnsAsync(false);

            var result = await _service.GetEventSkillsAsync(EventId, InstructorId, callerIsAdmin: false);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            _eventSkillRepository.Verify(r => r.GetActiveSkillsForEventAsync(It.IsAny<int>()), Times.Never);
        }

        // =====================================================================
        // Get: grouping
        // =====================================================================

        [TestMethod]
        public async Task Get_NoSkills_ReturnsSuccess_WithEmptyList()
        {
            _disciplines = [new Discipline { DisciplineId = 1, Name = "Swordplay" }];

            var result = await _service.GetEventSkillsAsync(EventId, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Data!.Count);
        }

        [TestMethod]
        public async Task Get_GroupsSkillsUnderTheirDiscipline()
        {
            _disciplines =
            [
                new Discipline { DisciplineId = 1, Name = "Swordplay" },
                new Discipline { DisciplineId = 2, Name = "Archery" }
            ];
            _eventActiveSkills =
            [
                KnownSkill(10, 1, "Parry"),
                KnownSkill(20, 2, "Longbow"),
                KnownSkill(11, 1, "Lunge")
            ];

            var result = await _service.GetEventSkillsAsync(EventId, AdminId, callerIsAdmin: true);

            var swordplay = result.Data!.Single(d => d.DisciplineId == 1);
            var archery = result.Data.Single(d => d.DisciplineId == 2);
            CollectionAssert.AreEquivalent(new[] { 10, 11 }, swordplay.Skills.Select(s => s.SkillId).ToList());
            CollectionAssert.AreEquivalent(new[] { 20 }, archery.Skills.Select(s => s.SkillId).ToList());
        }

        [TestMethod]
        public async Task Get_DisciplineWithNoneOfTheEventsSkills_IsOmitted()
        {
            _disciplines =
            [
                new Discipline { DisciplineId = 1, Name = "Swordplay" },
                new Discipline { DisciplineId = 2, Name = "Archery" }
            ];
            _eventActiveSkills = [KnownSkill(10, 1, "Parry")];

            var result = await _service.GetEventSkillsAsync(EventId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(1, result.Data!.Count);
            Assert.AreEqual(1, result.Data[0].DisciplineId);
        }

        [TestMethod]
        public async Task Get_SortsDisciplinesAndSkillsByName()
        {
            // Fed deliberately out of order
            _disciplines =
            [
                new Discipline { DisciplineId = 1, Name = "Swordplay" },
                new Discipline { DisciplineId = 2, Name = "Archery" }
            ];
            _eventActiveSkills =
            [
                KnownSkill(12, 1, "Riposte"),
                KnownSkill(10, 1, "Lunge"),
                KnownSkill(11, 1, "Parry"),
                KnownSkill(20, 2, "Longbow")
            ];

            var result = await _service.GetEventSkillsAsync(EventId, AdminId, callerIsAdmin: true);

            CollectionAssert.AreEqual(new[] { "Archery", "Swordplay" }, result.Data!.Select(d => d.Name).ToList());
            CollectionAssert.AreEqual(
                new[] { "Lunge", "Parry", "Riposte" },
                result.Data[1].Skills.Select(s => s.Name).ToList());
        }

        [TestMethod]
        public async Task Get_MapsSkillFields()
        {
            _disciplines = [new Discipline { DisciplineId = 1, Name = "Swordplay" }];
            var parry = KnownSkill(10, 1, "Parry");
            parry.Benefits = "Keeps you alive.";
            _eventActiveSkills = [parry];

            var result = await _service.GetEventSkillsAsync(EventId, AdminId, callerIsAdmin: true);

            var dto = result.Data!.Single().Skills.Single();
            Assert.AreEqual(10, dto.SkillId);
            Assert.AreEqual("Parry", dto.Name);
            Assert.AreEqual("Parry description", dto.Description);
            Assert.AreEqual("Keeps you alive.", dto.Benefits);
            Assert.AreEqual(1, dto.DisciplineId);
        }
    }
}