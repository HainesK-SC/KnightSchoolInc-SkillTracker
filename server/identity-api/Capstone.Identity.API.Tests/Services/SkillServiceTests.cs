using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class SkillServiceTests
    {
        private const int DisciplineId = 1;
        private const int OtherDisciplineId = 2;
        private const int SkillId = 10;
        private static readonly Guid AdminId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        private Mock<ISkillRepository> _skillRepository = null!;
        private Mock<IDisciplineRepository> _disciplineRepository = null!;
        private Mock<IParticipantSkillProgressRepository> _progressRepository = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private SkillService _service = null!;

        private Discipline? _addedDiscipline;
        private Skill? _addedSkill;

        // Records the order of staging calls and the save, for the delete tests
        private List<string> _callOrder = [];

        [TestInitialize]
        public void TestInitialize()
        {
            _addedDiscipline = null;
            _addedSkill = null;
            _callOrder = [];

            _disciplineRepository = new Mock<IDisciplineRepository>();
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(new List<Discipline>());
            _disciplineRepository
                .Setup(r => r.AddNewDiscipline(It.IsAny<Discipline>()))
                .Callback<Discipline>(d => _addedDiscipline = d);
            _disciplineRepository
                .Setup(r => r.RemoveDiscipline(It.IsAny<Discipline>()))
                .Callback(() => _callOrder.Add("RemoveDiscipline"));
            // Default: no name clashes
            _disciplineRepository
                .Setup(r => r.DisciplineExistsByName(It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(false);

            _skillRepository = new Mock<ISkillRepository>();
            _skillRepository
                .Setup(r => r.GetAllSkillsAsync())
                .ReturnsAsync(new List<Skill>());
            _skillRepository
                .Setup(r => r.GetAllSkillsInDisciplineAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Skill>());
            _skillRepository
                .Setup(r => r.AddNewSkill(It.IsAny<Skill>()))
                .Callback<Skill>(s => _addedSkill = s);
            _skillRepository
                .Setup(r => r.RemoveSkills(It.IsAny<IEnumerable<Skill>>()))
                .Callback(() => _callOrder.Add("RemoveSkills"));
            _skillRepository
                .Setup(r => r.ExistsByNameInDisciplineAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(false);

            // Default: nobody has progress on anything
            _progressRepository = new Mock<IParticipantSkillProgressRepository>();
            _progressRepository
                .Setup(r => r.AnyProgressInDisciplineAsync(It.IsAny<int>()))
                .ReturnsAsync(false);
            _progressRepository
                .Setup(r => r.AnyProgressForSkillAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork
                .Setup(u => u.SaveChangesAsync())
                .Callback(() => _callOrder.Add("Save"))
                .ReturnsAsync(1);

            _service = new SkillService(
                NullLogger<SkillService>.Instance,
                _skillRepository.Object,
                _disciplineRepository.Object,
                _unitOfWork.Object,
                _progressRepository.Object);
        }

        // ---------- Helpers ----------

        private static Discipline NewDiscipline(int id, string name)
        {
            return new Discipline { DisciplineId = id, Name = name };
        }

        private static Skill NewSkill(int id, int disciplineId, string name, bool isArchived = false)
        {
            return new Skill
            {
                SkillId = id,
                DisciplineId = disciplineId,
                Name = name,
                Description = $"{name} description",
                Benefits = null,
                IsArchived = isArchived
            };
        }

        // Makes the discipline exist for both the read-only and the tracked lookups
        private Discipline GivenDiscipline(int id, string name)
        {
            var discipline = NewDiscipline(id, name);
            _disciplineRepository.Setup(r => r.GetDisciplineByIdAsync(id)).ReturnsAsync(discipline);
            _disciplineRepository.Setup(r => r.GetDisciplineByIdForUpdate(id)).ReturnsAsync(discipline);
            return discipline;
        }

        // Makes the skill exist for both the read-only and the tracked lookups
        private Skill GivenSkill(int id, int disciplineId, string name, bool isArchived = false)
        {
            var skill = NewSkill(id, disciplineId, name, isArchived);
            _skillRepository.Setup(r => r.GetSkillByIdAsync(id)).ReturnsAsync(skill);
            _skillRepository.Setup(r => r.GetSkillByIdForUpdate(id)).ReturnsAsync(skill);
            return skill;
        }

        private static SkillRequestDto SkillRequest(
            string name = "Parry",
            string description = "Deflect a strike.",
            string? benefits = null,
            int? disciplineId = DisciplineId)
        {
            return new SkillRequestDto
            {
                Name = name,
                Description = description,
                Benefits = benefits,
                DisciplineId = disciplineId
            };
        }

        private void VerifyNeverSaved()
        {
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        private void VerifySavedOnce()
        {
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // =====================================================================
        // Disciplines: reads
        // =====================================================================

        [TestMethod]
        public async Task GetDisciplines_ReturnsAllMapped()
        {
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(new List<Discipline> { NewDiscipline(1, "Archery"), NewDiscipline(2, "Swordplay") });

            var result = await _service.GetDisciplinesAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(2, result.Data!.Count);
            Assert.AreEqual(1, result.Data[0].DisciplineId);
            Assert.AreEqual("Archery", result.Data[0].Name);
            Assert.AreEqual("Swordplay", result.Data[1].Name);
        }

        [TestMethod]
        public async Task GetDisciplines_None_ReturnsSuccess_WithEmptyList()
        {
            var result = await _service.GetDisciplinesAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Data!.Count);
        }

        [TestMethod]
        public async Task GetDiscipline_Found_ReturnsMappedDto()
        {
            GivenDiscipline(DisciplineId, "Archery");

            var result = await _service.GetDisciplineAsync(DisciplineId);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(DisciplineId, result.Data!.DisciplineId);
            Assert.AreEqual("Archery", result.Data.Name);
        }

        [TestMethod]
        public async Task GetDiscipline_Missing_ReturnsNotFound()
        {
            var result = await _service.GetDisciplineAsync(999);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        // =====================================================================
        // Disciplines: create
        // =====================================================================

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public async Task CreateDiscipline_BlankName_ReturnsValidation_AndDoesNotAdd(string name)
        {
            var result = await _service.CreateDisciplineAsync(new DisciplineRequestDto { Name = name }, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            _disciplineRepository.Verify(r => r.AddNewDiscipline(It.IsAny<Discipline>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task CreateDiscipline_DuplicateName_ReturnsConflict_AndDoesNotAdd()
        {
            _disciplineRepository
                .Setup(r => r.DisciplineExistsByName("Archery", It.IsAny<int?>()))
                .ReturnsAsync(true);

            var result = await _service.CreateDisciplineAsync(new DisciplineRequestDto { Name = "Archery" }, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            _disciplineRepository.Verify(r => r.AddNewDiscipline(It.IsAny<Discipline>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task CreateDiscipline_ChecksDuplicate_UsingTrimmedName()
        {
            await _service.CreateDisciplineAsync(new DisciplineRequestDto { Name = "  Archery  " }, AdminId);

            _disciplineRepository.Verify(r => r.DisciplineExistsByName("Archery", It.IsAny<int?>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateDiscipline_Success_StoresTrimmedName_AndSavesOnce()
        {
            // Regression: the trimmed name was checked but the untrimmed name was stored
            var result = await _service.CreateDisciplineAsync(new DisciplineRequestDto { Name = "  Archery  " }, AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_addedDiscipline);
            Assert.AreEqual("Archery", _addedDiscipline.Name);
            Assert.AreEqual("Archery", result.Data!.Name);
            VerifySavedOnce();
        }

        // =====================================================================
        // Disciplines: update
        // =====================================================================

        [TestMethod]
        public async Task UpdateDiscipline_Missing_ReturnsNotFound_AndDoesNotSave()
        {
            var result = await _service.UpdateDisciplineAsync(999, new DisciplineRequestDto { Name = "Archery" }, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNeverSaved();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public async Task UpdateDiscipline_BlankName_ReturnsValidation_AndLeavesNameUnchanged(string name)
        {
            var discipline = GivenDiscipline(DisciplineId, "Archery");

            var result = await _service.UpdateDisciplineAsync(DisciplineId, new DisciplineRequestDto { Name = name }, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            Assert.AreEqual("Archery", discipline.Name);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task UpdateDiscipline_DuplicateName_ReturnsConflict_AndLeavesNameUnchanged()
        {
            var discipline = GivenDiscipline(DisciplineId, "Archery");
            _disciplineRepository
                .Setup(r => r.DisciplineExistsByName("Swordplay", DisciplineId))
                .ReturnsAsync(true);

            var result = await _service.UpdateDisciplineAsync(DisciplineId, new DisciplineRequestDto { Name = "Swordplay" }, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            Assert.AreEqual("Archery", discipline.Name);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task UpdateDiscipline_DuplicateCheck_ExcludesItsOwnId()
        {
            // Without excludingId, keeping (or re-casing) its own name would clash with itself
            GivenDiscipline(DisciplineId, "Archery");

            await _service.UpdateDisciplineAsync(DisciplineId, new DisciplineRequestDto { Name = "  Archery  " }, AdminId);

            _disciplineRepository.Verify(r => r.DisciplineExistsByName("Archery", DisciplineId), Times.Once);
        }

        [TestMethod]
        public async Task UpdateDiscipline_Success_StoresTrimmedName_AndSavesOnce()
        {
            var discipline = GivenDiscipline(DisciplineId, "Archery");

            var result = await _service.UpdateDisciplineAsync(DisciplineId, new DisciplineRequestDto { Name = "  Marksmanship  " }, AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("Marksmanship", discipline.Name);
            Assert.AreEqual("Marksmanship", result.Data!.Name);
            VerifySavedOnce();
        }

        // =====================================================================
        // Disciplines: delete
        // =====================================================================

        [TestMethod]
        public async Task DeleteDiscipline_Missing_ReturnsNotFound_AndRemovesNothing()
        {
            var result = await _service.DeleteDisciplineAsync(999, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            _disciplineRepository.Verify(r => r.RemoveDiscipline(It.IsAny<Discipline>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task DeleteDiscipline_ParticipantsHaveProgress_ReturnsConflict_AndRemovesNothing()
        {
            GivenDiscipline(DisciplineId, "Swordplay");
            _progressRepository
                .Setup(r => r.AnyProgressInDisciplineAsync(DisciplineId))
                .ReturnsAsync(true);

            var result = await _service.DeleteDisciplineAsync(DisciplineId, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            _skillRepository.Verify(r => r.RemoveSkills(It.IsAny<IEnumerable<Skill>>()), Times.Never);
            _disciplineRepository.Verify(r => r.RemoveDiscipline(It.IsAny<Discipline>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task DeleteDiscipline_ChecksProgress_ForThatDiscipline()
        {
            GivenDiscipline(DisciplineId, "Swordplay");

            await _service.DeleteDisciplineAsync(DisciplineId, AdminId);

            _progressRepository.Verify(r => r.AnyProgressInDisciplineAsync(DisciplineId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteDiscipline_NoProgress_RemovesAllItsSkills_IncludingArchived()
        {
            var discipline = GivenDiscipline(DisciplineId, "Swordplay");
            var skills = new List<Skill>
            {
                NewSkill(10, DisciplineId, "Parry"),
                NewSkill(11, DisciplineId, "Lunge", isArchived: true)
            };
            _skillRepository
                .Setup(r => r.GetAllSkillsInDisciplineAsync(DisciplineId))
                .ReturnsAsync(skills);

            IEnumerable<Skill>? removedSkills = null;
            _skillRepository
                .Setup(r => r.RemoveSkills(It.IsAny<IEnumerable<Skill>>()))
                .Callback<IEnumerable<Skill>>(s =>
                {
                    removedSkills = s;
                    _callOrder.Add("RemoveSkills");
                });

            var result = await _service.DeleteDisciplineAsync(DisciplineId, AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(removedSkills);
            CollectionAssert.AreEquivalent(skills, removedSkills.ToList());
            _disciplineRepository.Verify(r => r.RemoveDiscipline(discipline), Times.Once);
        }

        [TestMethod]
        public async Task DeleteDiscipline_NoProgress_StagesEverything_ThenSavesOnce()
        {
            GivenDiscipline(DisciplineId, "Swordplay");
            _skillRepository
                .Setup(r => r.GetAllSkillsInDisciplineAsync(DisciplineId))
                .ReturnsAsync(new List<Skill> { NewSkill(10, DisciplineId, "Parry") });

            await _service.DeleteDisciplineAsync(DisciplineId, AdminId);

            // One save, and it comes after both removals are staged
            CollectionAssert.AreEqual(
                new[] { "RemoveSkills", "RemoveDiscipline", "Save" },
                _callOrder);
        }

        [TestMethod]
        public async Task DeleteDiscipline_WithNoSkills_StillSucceeds()
        {
            var discipline = GivenDiscipline(DisciplineId, "Swordplay");

            var result = await _service.DeleteDisciplineAsync(DisciplineId, AdminId);

            Assert.IsTrue(result.Succeeded);
            _disciplineRepository.Verify(r => r.RemoveDiscipline(discipline), Times.Once);
            VerifySavedOnce();
        }

        // =====================================================================
        // Catalog
        // =====================================================================

        [TestMethod]
        public async Task GetCatalog_GroupsSkillsUnderTheirDiscipline()
        {
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(new List<Discipline> { NewDiscipline(1, "Archery"), NewDiscipline(2, "Swordplay") });
            _skillRepository
                .Setup(r => r.GetAllSkillsAsync())
                .ReturnsAsync(new List<Skill>
                {
                    NewSkill(10, 2, "Parry"),
                    NewSkill(11, 1, "Longbow"),
                    NewSkill(12, 2, "Lunge")
                });

            var result = await _service.GetCatalogAsync();

            Assert.IsTrue(result.Succeeded);
            var archery = result.Data!.Single(d => d.DisciplineId == 1);
            var swordplay = result.Data.Single(d => d.DisciplineId == 2);
            CollectionAssert.AreEquivalent(new[] { 11 }, archery.Skills.Select(s => s.SkillId).ToList());
            CollectionAssert.AreEquivalent(new[] { 10, 12 }, swordplay.Skills.Select(s => s.SkillId).ToList());
        }

        [TestMethod]
        public async Task GetCatalog_DisciplineWithNoSkills_AppearsWithEmptyList()
        {
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(new List<Discipline> { NewDiscipline(1, "Archery"), NewDiscipline(2, "Swordplay") });
            _skillRepository
                .Setup(r => r.GetAllSkillsAsync())
                .ReturnsAsync(new List<Skill> { NewSkill(10, 2, "Parry") });

            var result = await _service.GetCatalogAsync();

            var archery = result.Data!.Single(d => d.DisciplineId == 1);
            Assert.AreEqual(0, archery.Skills.Count);
        }

        [TestMethod]
        public async Task GetCatalog_NoSkillsAnywhere_StillListsEveryDiscipline()
        {
            // Regression: an early return dropped every discipline when no skills existed
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(new List<Discipline> { NewDiscipline(1, "Archery"), NewDiscipline(2, "Swordplay") });

            var result = await _service.GetCatalogAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(2, result.Data!.Count);
            Assert.IsTrue(result.Data.All(d => d.Skills.Count == 0));
        }

        [TestMethod]
        public async Task GetCatalog_IncludesArchivedSkills_WithFlagSet()
        {
            _disciplineRepository
                .Setup(r => r.GetAllDisciplinesAsync())
                .ReturnsAsync(new List<Discipline> { NewDiscipline(1, "Swordplay") });
            _skillRepository
                .Setup(r => r.GetAllSkillsAsync())
                .ReturnsAsync(new List<Skill>
                {
                    NewSkill(10, 1, "Parry"),
                    NewSkill(11, 1, "Lunge", isArchived: true)
                });

            var result = await _service.GetCatalogAsync();

            var skills = result.Data!.Single().Skills;
            Assert.AreEqual(2, skills.Count);
            Assert.IsTrue(skills.Single(s => s.SkillId == 11).IsArchived);
            Assert.IsFalse(skills.Single(s => s.SkillId == 10).IsArchived);
        }

        [TestMethod]
        public async Task GetCatalog_NoDisciplines_ReturnsSuccess_WithEmptyList()
        {
            var result = await _service.GetCatalogAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Data!.Count);
        }

        // =====================================================================
        // Skills: read
        // =====================================================================

        [TestMethod]
        public async Task GetSkill_Found_ReturnsMappedDto()
        {
            GivenSkill(SkillId, DisciplineId, "Parry", isArchived: true);

            var result = await _service.GetSkillAsync(SkillId);

            Assert.IsTrue(result.Succeeded);
            var dto = result.Data!;
            Assert.AreEqual(SkillId, dto.SkillId);
            Assert.AreEqual("Parry", dto.Name);
            Assert.AreEqual("Parry description", dto.Description);
            Assert.AreEqual(DisciplineId, dto.DisciplineId);
            Assert.IsTrue(dto.IsArchived);
        }

        [TestMethod]
        public async Task GetSkill_Missing_ReturnsNotFound()
        {
            var result = await _service.GetSkillAsync(999);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        // =====================================================================
        // Skills: create
        // =====================================================================

        [TestMethod]
        public async Task CreateSkill_DisciplineMissing_ReturnsValidation_AndDoesNotAdd()
        {
            var result = await _service.CreateSkillAsync(SkillRequest(disciplineId: 999), AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            _skillRepository.Verify(r => r.AddNewSkill(It.IsAny<Skill>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task CreateSkill_DuplicateNameInDiscipline_ReturnsConflict_AndDoesNotAdd()
        {
            GivenDiscipline(DisciplineId, "Swordplay");
            _skillRepository
                .Setup(r => r.ExistsByNameInDisciplineAsync(DisciplineId, "Parry", It.IsAny<int?>()))
                .ReturnsAsync(true);

            var result = await _service.CreateSkillAsync(SkillRequest(name: "Parry"), AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            _skillRepository.Verify(r => r.AddNewSkill(It.IsAny<Skill>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task CreateSkill_ChecksDuplicate_UsingTrimmedSkillName_NotDisciplineName()
        {
            // Regression: the duplicate check used the discipline's name instead of the skill's
            GivenDiscipline(DisciplineId, "Swordplay");

            await _service.CreateSkillAsync(SkillRequest(name: "  Parry  "), AdminId);

            _skillRepository.Verify(r => r.ExistsByNameInDisciplineAsync(DisciplineId, "Parry", It.IsAny<int?>()), Times.Once);
            _skillRepository.Verify(r => r.ExistsByNameInDisciplineAsync(It.IsAny<int>(), "Swordplay", It.IsAny<int?>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateSkill_Success_StoresRequestValues_Trimmed_AndActive()
        {
            // Regression: the skill was stored with the discipline's name
            GivenDiscipline(DisciplineId, "Swordplay");

            var result = await _service.CreateSkillAsync(
                SkillRequest(name: "  Parry  ", description: "  Deflect a strike.  ", benefits: "  Keeps you alive.  "),
                AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_addedSkill);
            Assert.AreEqual("Parry", _addedSkill.Name);
            Assert.AreEqual("Deflect a strike.", _addedSkill.Description);
            Assert.AreEqual("Keeps you alive.", _addedSkill.Benefits);
            Assert.AreEqual(DisciplineId, _addedSkill.DisciplineId);
            Assert.IsFalse(_addedSkill.IsArchived);
            VerifySavedOnce();
        }

        [TestMethod]
        [DataRow((string?)null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task CreateSkill_BlankBenefits_StoredAsNull(string? benefits)
        {
            GivenDiscipline(DisciplineId, "Swordplay");

            await _service.CreateSkillAsync(SkillRequest(benefits: benefits), AdminId);

            Assert.IsNotNull(_addedSkill);
            Assert.IsNull(_addedSkill.Benefits);
        }

        [TestMethod]
        public async Task CreateSkill_Success_ReturnsMappedDto()
        {
            GivenDiscipline(DisciplineId, "Swordplay");

            var result = await _service.CreateSkillAsync(SkillRequest(name: "Parry"), AdminId);

            var dto = result.Data!;
            Assert.AreEqual("Parry", dto.Name);
            Assert.AreEqual(DisciplineId, dto.DisciplineId);
            Assert.IsFalse(dto.IsArchived);
        }

        // =====================================================================
        // Skills: update
        // =====================================================================

        [TestMethod]
        public async Task UpdateSkill_Missing_ReturnsNotFound_AndDoesNotSave()
        {
            GivenDiscipline(DisciplineId, "Swordplay");

            var result = await _service.UpdateSkillAsync(999, SkillRequest(), AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task UpdateSkill_TargetDisciplineMissing_ReturnsValidation_AndLeavesSkillUnchanged()
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry");

            var result = await _service.UpdateSkillAsync(SkillId, SkillRequest(name: "Riposte", disciplineId: 999), AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            Assert.AreEqual("Parry", skill.Name);
            Assert.AreEqual(DisciplineId, skill.DisciplineId);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task UpdateSkill_DuplicateNameInTargetDiscipline_ReturnsConflict_AndLeavesSkillUnchanged()
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry");
            GivenDiscipline(OtherDisciplineId, "Fencing");
            _skillRepository
                .Setup(r => r.ExistsByNameInDisciplineAsync(OtherDisciplineId, "Parry", SkillId))
                .ReturnsAsync(true);

            var result = await _service.UpdateSkillAsync(SkillId, SkillRequest(name: "Parry", disciplineId: OtherDisciplineId), AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            Assert.AreEqual(DisciplineId, skill.DisciplineId);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task UpdateSkill_DuplicateCheck_UsesTargetDiscipline_TrimmedName_AndExcludesItself()
        {
            GivenSkill(SkillId, DisciplineId, "Parry");
            GivenDiscipline(OtherDisciplineId, "Fencing");

            await _service.UpdateSkillAsync(SkillId, SkillRequest(name: "  Parry  ", disciplineId: OtherDisciplineId), AdminId);

            _skillRepository.Verify(r => r.ExistsByNameInDisciplineAsync(OtherDisciplineId, "Parry", SkillId), Times.Once);
        }

        [TestMethod]
        public async Task UpdateSkill_Success_UpdatesAllFields_Trimmed_AndSavesOnce()
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry");
            GivenDiscipline(DisciplineId, "Swordplay");

            var result = await _service.UpdateSkillAsync(
                SkillId,
                SkillRequest(name: "  Riposte  ", description: "  Counter-attack.  ", benefits: "  Ends fights.  "),
                AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("Riposte", skill.Name);
            Assert.AreEqual("Counter-attack.", skill.Description);
            Assert.AreEqual("Ends fights.", skill.Benefits);
            Assert.AreEqual("Riposte", result.Data!.Name);
            VerifySavedOnce();
        }

        [TestMethod]
        public async Task UpdateSkill_MoveToAnotherDiscipline_ChangesDisciplineId()
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry");
            GivenDiscipline(OtherDisciplineId, "Fencing");

            var result = await _service.UpdateSkillAsync(SkillId, SkillRequest(name: "Parry", disciplineId: OtherDisciplineId), AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(OtherDisciplineId, skill.DisciplineId);
            Assert.AreEqual(OtherDisciplineId, result.Data!.DisciplineId);
            VerifySavedOnce();
        }

        [TestMethod]
        public async Task UpdateSkill_BlankBenefits_ClearsToNull()
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry");
            skill.Benefits = "Old blurb.";
            GivenDiscipline(DisciplineId, "Swordplay");

            await _service.UpdateSkillAsync(SkillId, SkillRequest(benefits: "   "), AdminId);

            Assert.IsNull(skill.Benefits);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task UpdateSkill_NeverChangesIsArchived(bool isArchived)
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry", isArchived);
            GivenDiscipline(DisciplineId, "Swordplay");

            var result = await _service.UpdateSkillAsync(SkillId, SkillRequest(name: "Riposte"), AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(isArchived, skill.IsArchived);
            Assert.AreEqual(isArchived, result.Data!.IsArchived);
        }

        // =====================================================================
        // Skills: archive / unarchive
        // =====================================================================

        [TestMethod]
        public async Task Archive_Missing_ReturnsNotFound()
        {
            var result = await _service.ArchiveSkillAsync(999, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Archive_ActiveSkill_SetsFlag_AndSavesOnce()
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry", isArchived: false);

            var result = await _service.ArchiveSkillAsync(SkillId, AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(skill.IsArchived);
            Assert.IsTrue(result.Data!.IsArchived);
            VerifySavedOnce();
        }

        [TestMethod]
        public async Task Archive_AlreadyArchived_ReturnsSuccess_AndDoesNotSave()
        {
            GivenSkill(SkillId, DisciplineId, "Parry", isArchived: true);

            var result = await _service.ArchiveSkillAsync(SkillId, AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.Data!.IsArchived);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Unarchive_Missing_ReturnsNotFound()
        {
            var result = await _service.UnarchiveSkillAsync(999, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Unarchive_ArchivedSkill_ClearsFlag_AndSavesOnce()
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry", isArchived: true);

            var result = await _service.UnarchiveSkillAsync(SkillId, AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(skill.IsArchived);
            Assert.IsFalse(result.Data!.IsArchived);
            VerifySavedOnce();
        }

        [TestMethod]
        public async Task Unarchive_AlreadyActive_ReturnsSuccess_AndDoesNotSave()
        {
            GivenSkill(SkillId, DisciplineId, "Parry", isArchived: false);

            var result = await _service.UnarchiveSkillAsync(SkillId, AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.Data!.IsArchived);
            VerifyNeverSaved();
        }

        // =====================================================================
        // Skills: delete
        // =====================================================================

        [TestMethod]
        public async Task DeleteSkill_Missing_ReturnsNotFound_AndRemovesNothing()
        {
            var result = await _service.DeleteSkillAsync(999, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            _skillRepository.Verify(r => r.RemoveSkill(It.IsAny<Skill>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DeleteSkill_ParticipantsHaveProgress_ReturnsConflict_EvenIfArchived(bool isArchived)
        {
            GivenSkill(SkillId, DisciplineId, "Parry", isArchived);
            _progressRepository
                .Setup(r => r.AnyProgressForSkillAsync(SkillId))
                .ReturnsAsync(true);

            var result = await _service.DeleteSkillAsync(SkillId, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            _skillRepository.Verify(r => r.RemoveSkill(It.IsAny<Skill>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DeleteSkill_NoProgress_RemovesSkill_AndSavesOnce(bool isArchived)
        {
            var skill = GivenSkill(SkillId, DisciplineId, "Parry", isArchived);

            var result = await _service.DeleteSkillAsync(SkillId, AdminId);

            Assert.IsTrue(result.Succeeded);
            _skillRepository.Verify(r => r.RemoveSkill(skill), Times.Once);
            _progressRepository.Verify(r => r.AnyProgressForSkillAsync(SkillId), Times.Once);
            VerifySavedOnce();
        }
    }
}