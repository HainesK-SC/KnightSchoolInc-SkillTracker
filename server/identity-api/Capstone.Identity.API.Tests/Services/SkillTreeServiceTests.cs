using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class SkillTreeServiceTests
    {
        private const int UserProfileId = 42;
        private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid MarkerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly DateTime MarkedAtUtc = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        private Mock<IUserProfileRepository> _userProfileRepository = null!;
        private Mock<IDisciplineRepository> _disciplineRepository = null!;
        private Mock<ISkillRepository> _skillRepository = null!;
        private Mock<IParticipantSkillProgressRepository> _progressRepository = null!;
        private SkillTreeService _service = null!;

        // What the mocked repositories return. Tests add to these before calling the service.
        private List<Discipline> _disciplines = [];
        private List<Skill> _activeSkills = [];
        private List<ParticipantSkillProgress> _progressRows = [];

        [TestInitialize]
        public void TestInitialize()
        {
            _disciplines = [];
            _activeSkills = [];
            _progressRows = [];

            _userProfileRepository = new Mock<IUserProfileRepository>();
            _userProfileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(UserId))
                .ReturnsAsync(new UserProfile { UserProfileId = UserProfileId, ApplicationUserId = UserId });

            // The lambdas read the lists at call time, so tests can fill them after setup
            _disciplineRepository = new Mock<IDisciplineRepository>();
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(() => _disciplines);

            _skillRepository = new Mock<ISkillRepository>();
            _skillRepository
                .Setup(r => r.GetAllActiveSkills())
                .ReturnsAsync(() => _activeSkills);

            _progressRepository = new Mock<IParticipantSkillProgressRepository>();
            _progressRepository
                .Setup(r => r.GetForUserAsync(UserProfileId))
                .ReturnsAsync(() => _progressRows);

            _service = new SkillTreeService(
                _userProfileRepository.Object,
                _disciplineRepository.Object,
                _skillRepository.Object,
                _progressRepository.Object);
        }

        // ---------- Helpers ----------

        private Discipline AddDiscipline(int id, string name)
        {
            var discipline = new Discipline { DisciplineId = id, Name = name };
            _disciplines.Add(discipline);
            return discipline;
        }

        private Skill AddActiveSkill(int id, int disciplineId, string name, string? benefits = null)
        {
            var skill = new Skill
            {
                SkillId = id,
                DisciplineId = disciplineId,
                Name = name,
                Description = $"{name} description",
                Benefits = benefits,
                IsArchived = false
            };
            _activeSkills.Add(skill);
            return skill;
        }

        private void AddProgress(int skillId, SkillStatus status)
        {
            _progressRows.Add(new ParticipantSkillProgress
            {
                UserProfileId = UserProfileId,
                SkillId = skillId,
                Status = status,
                UpdatedAtUtc = MarkedAtUtc,
                UpdatedById = MarkerId
            });
        }

        private async Task<SkillTreeDto> GetTree()
        {
            var result = await _service.GetSkillTreeAsync(UserId);
            Assert.IsTrue(result.Succeeded, $"Expected success but got {result.ErrorType}: {result.Error}");
            return result.Data!;
        }

        private static SkillTreeSkillDto FindSkill(SkillTreeDto tree, int skillId)
        {
            return tree.Disciplines.SelectMany(d => d.Skills).Single(s => s.SkillId == skillId);
        }

        // ---------- Participant lookup ----------

        [TestMethod]
        public async Task ProfileNotFound_ReturnsNotFound_AndLoadsNothingElse()
        {
            _userProfileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(UserId))
                .ReturnsAsync((UserProfile?)null);

            var result = await _service.GetSkillTreeAsync(UserId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            _disciplineRepository.Verify(r => r.GetAllDisciplinesAsync(), Times.Never);
            _skillRepository.Verify(r => r.GetAllActiveSkills(), Times.Never);
            _progressRepository.Verify(r => r.GetForUserAsync(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task LoadsProgress_ByProfileId()
        {
            await GetTree();

            _progressRepository.Verify(r => r.GetForUserAsync(UserProfileId), Times.Once);
        }

        // ---------- Empty cases ----------

        [TestMethod]
        public async Task NoDisciplinesOrSkills_ReturnsSuccess_WithEmptyList()
        {
            var tree = await GetTree();

            Assert.AreEqual(UserId, tree.UserId);
            Assert.AreEqual(0, tree.Disciplines.Count);
        }

        [TestMethod]
        public async Task DisciplinesButNoActiveSkills_ReturnsSuccess_WithEmptyList()
        {
            AddDiscipline(1, "Archery");
            AddDiscipline(2, "Swordplay");

            var tree = await GetTree();

            Assert.AreEqual(0, tree.Disciplines.Count);
        }

        // ---------- Status ----------

        [TestMethod]
        public async Task SkillWithNoProgressRow_IsLocked_WithNullTime()
        {
            AddDiscipline(1, "Swordplay");
            AddActiveSkill(10, 1, "Parry");

            var tree = await GetTree();

            var skill = FindSkill(tree, 10);
            Assert.AreEqual(SkillStatus.Locked, skill.Status);
            Assert.IsNull(skill.UpdatedAtUtc);
        }

        [TestMethod]
        [DataRow(SkillStatus.InProgress)]
        [DataRow(SkillStatus.Completed)]
        public async Task SkillWithProgressRow_UsesRowStatusAndTime(SkillStatus status)
        {
            AddDiscipline(1, "Swordplay");
            AddActiveSkill(10, 1, "Parry");
            AddProgress(10, status);

            var tree = await GetTree();

            var skill = FindSkill(tree, 10);
            Assert.AreEqual(status, skill.Status);
            Assert.AreEqual(MarkedAtUtc, skill.UpdatedAtUtc);
        }

        [TestMethod]
        public async Task MixedProgress_EachSkillGetsItsOwnStatus()
        {
            AddDiscipline(1, "Swordplay");
            AddActiveSkill(10, 1, "Lunge");
            AddActiveSkill(11, 1, "Parry");
            AddActiveSkill(12, 1, "Riposte");
            AddProgress(10, SkillStatus.InProgress);
            AddProgress(12, SkillStatus.Completed);

            var tree = await GetTree();

            Assert.AreEqual(SkillStatus.InProgress, FindSkill(tree, 10).Status);
            Assert.AreEqual(SkillStatus.Locked, FindSkill(tree, 11).Status);
            Assert.AreEqual(SkillStatus.Completed, FindSkill(tree, 12).Status);
        }

        // ---------- Archived / hidden ----------

        [TestMethod]
        public async Task ProgressOnSkillNotInActiveList_IsIgnored()
        {
            // Skill 99 is archived, so the active-skills read never returns it,
            // but the participant still has a progress row for it.
            AddDiscipline(1, "Swordplay");
            AddActiveSkill(10, 1, "Parry");
            AddProgress(99, SkillStatus.Completed);

            var tree = await GetTree();

            var allSkills = tree.Disciplines.SelectMany(d => d.Skills).ToList();
            Assert.AreEqual(1, allSkills.Count);
            Assert.AreEqual(10, allSkills[0].SkillId);
            Assert.AreEqual(SkillStatus.Locked, allSkills[0].Status);
        }

        [TestMethod]
        public async Task DisciplineWithNoActiveSkills_IsOmitted()
        {
            AddDiscipline(1, "Swordplay");
            AddDiscipline(2, "Archery");        // no active skills (all archived, or none at all)
            AddActiveSkill(10, 1, "Parry");

            var tree = await GetTree();

            Assert.AreEqual(1, tree.Disciplines.Count);
            Assert.AreEqual(1, tree.Disciplines[0].DisciplineId);
        }

        // ---------- Grouping and mapping ----------

        [TestMethod]
        public async Task Skills_AreGroupedUnderTheirOwnDiscipline()
        {
            AddDiscipline(1, "Archery");
            AddDiscipline(2, "Swordplay");
            AddActiveSkill(10, 2, "Parry");
            AddActiveSkill(11, 1, "Longbow");
            AddActiveSkill(12, 2, "Lunge");

            var tree = await GetTree();

            var archery = tree.Disciplines.Single(d => d.DisciplineId == 1);
            var swordplay = tree.Disciplines.Single(d => d.DisciplineId == 2);

            CollectionAssert.AreEquivalent(new[] { 11 }, archery.Skills.Select(s => s.SkillId).ToList());
            CollectionAssert.AreEquivalent(new[] { 10, 12 }, swordplay.Skills.Select(s => s.SkillId).ToList());
        }

        [TestMethod]
        public async Task Discipline_MapsIdAndName()
        {
            AddDiscipline(5, "Horsemanship");
            AddActiveSkill(10, 5, "Gallop");

            var tree = await GetTree();

            Assert.AreEqual(5, tree.Disciplines[0].DisciplineId);
            Assert.AreEqual("Horsemanship", tree.Disciplines[0].Name);
        }

        [TestMethod]
        public async Task Skill_MapsNameDescriptionAndBenefits()
        {
            AddDiscipline(1, "Swordplay");
            AddActiveSkill(10, 1, "Parry", benefits: "Keeps you alive.");
            AddActiveSkill(11, 1, "Lunge");     // no benefits blurb

            var tree = await GetTree();

            var parry = FindSkill(tree, 10);
            Assert.AreEqual("Parry", parry.Name);
            Assert.AreEqual("Parry description", parry.Description);
            Assert.AreEqual("Keeps you alive.", parry.Benefits);

            Assert.IsNull(FindSkill(tree, 11).Benefits);
        }

        // ---------- Sorting ----------
        // The repositories return the lists deliberately out of order,
        // so these only pass if the service sorts them itself.

        [TestMethod]
        public async Task Disciplines_AreSortedByName()
        {
            AddDiscipline(1, "Swordplay");
            AddDiscipline(2, "Archery");
            AddDiscipline(3, "Horsemanship");
            AddActiveSkill(10, 1, "Parry");
            AddActiveSkill(11, 2, "Longbow");
            AddActiveSkill(12, 3, "Gallop");

            var tree = await GetTree();

            CollectionAssert.AreEqual(
                new[] { "Archery", "Horsemanship", "Swordplay" },
                tree.Disciplines.Select(d => d.Name).ToList());
        }

        [TestMethod]
        public async Task Skills_AreSortedByName_WithinEachDiscipline()
        {
            AddDiscipline(1, "Swordplay");
            AddActiveSkill(10, 1, "Riposte");
            AddActiveSkill(11, 1, "Lunge");
            AddActiveSkill(12, 1, "Parry");

            var tree = await GetTree();

            CollectionAssert.AreEqual(
                new[] { "Lunge", "Parry", "Riposte" },
                tree.Disciplines[0].Skills.Select(s => s.Name).ToList());
        }
    }
}