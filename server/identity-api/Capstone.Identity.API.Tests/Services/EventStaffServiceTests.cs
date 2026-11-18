using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Models.ReadModels;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Capstone.Identity.API.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class EventStaffServiceTests
    {
        private const int EventId = 5;
        private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid AdminId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        private Mock<IEventStaffRepository> _staffRepository = null!;
        private Mock<IEventRepository> _eventRepository = null!;
        private Mock<IUserProfileRepository> _userProfileRepository = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<UserManager<ApplicationUser>> _userManager = null!;
        private EventStaffService _service = null!;

        private ApplicationUser _user = null!;
        private EventStaffAssignment? _addedAssignment;
        private List<EventStaffMember> _staffList = [];
        private List<string> _callOrder = [];

        [TestInitialize]
        public void TestInitialize()
        {
            _addedAssignment = null;
            _staffList = [];
            _callOrder = [];

            // Default: the event exists
            _eventRepository = new Mock<IEventRepository>();
            _eventRepository
                .Setup(r => r.GetEventByIdAsync(EventId))
                .ReturnsAsync(new Event { EventId = EventId, EventName = "Spring Tournament", EventLocation = "Toronto" });

            // Default: the user exists and is an instructor (not an admin)
            _user = new ApplicationUser { Id = UserId, FirstName = "Kyle", LastName = "Test" };
            _userManager = IdentityMocks.CreateUserManager();
            _userManager
                .Setup(m => m.FindByIdAsync(UserId.ToString()))
                .ReturnsAsync(_user);
            _userManager
                .Setup(m => m.IsInRoleAsync(_user, Roles.Instructor))
                .ReturnsAsync(true);
            _userManager
                .Setup(m => m.IsInRoleAsync(_user, Roles.Admin))
                .ReturnsAsync(false);

            // Default: not assigned yet; the staff list reads whatever _staffList holds at call time
            _staffRepository = new Mock<IEventStaffRepository>();
            _staffRepository
                .Setup(r => r.IsAssignedAsync(EventId, UserId))
                .ReturnsAsync(false);
            _staffRepository
                .Setup(r => r.GetEventStaffAssignmentForUpdateAsync(EventId, UserId))
                .ReturnsAsync((EventStaffAssignment?)null);
            _staffRepository
                .Setup(r => r.AddStaffAssignmentAsync(It.IsAny<EventStaffAssignment>()))
                .Callback<EventStaffAssignment>(a => _addedAssignment = a)
                .ReturnsAsync((EventStaffAssignment a) => a);
            _staffRepository
                .Setup(r => r.RemoveStaffAssignment(It.IsAny<EventStaffAssignment>()))
                .Callback(() => _callOrder.Add("Remove"));
            _staffRepository
                .Setup(r => r.GetEventStaffForEventAsync(EventId))
                .Callback(() => _callOrder.Add("LoadStaff"))
                .ReturnsAsync(() => _staffList);

            _userProfileRepository = new Mock<IUserProfileRepository>();

            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork
                .Setup(u => u.SaveChangesAsync())
                .Callback(() => _callOrder.Add("Save"))
                .ReturnsAsync(1);

            _service = new EventStaffService(
                _staffRepository.Object,
                _eventRepository.Object,
                _userProfileRepository.Object,
                NullLogger<EventStaffService>.Instance,
                _unitOfWork.Object,
                _userManager.Object);
        }

        // ---------- Helpers ----------

        private void GivenEventMissing()
        {
            _eventRepository
                .Setup(r => r.GetEventByIdAsync(EventId))
                .ReturnsAsync((Event?)null);
        }

        private void GivenUserMissing()
        {
            _userManager
                .Setup(m => m.FindByIdAsync(UserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);
        }

        private EventStaffAssignment GivenAlreadyAssigned()
        {
            var assignment = new EventStaffAssignment
            {
                Id = 3,
                EventId = EventId,
                UserId = UserId,
                AssignedAtUtc = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc),
                AssignedBy = AdminId
            };

            _staffRepository.Setup(r => r.IsAssignedAsync(EventId, UserId)).ReturnsAsync(true);
            _staffRepository.Setup(r => r.GetEventStaffAssignmentForUpdateAsync(EventId, UserId)).ReturnsAsync(assignment);

            return assignment;
        }

        private static EventStaffMember StaffMember(Guid userId, string firstName, string lastName)
        {
            return new EventStaffMember
            {
                Id = 1,
                EventId = EventId,
                UserId = userId,
                FirstName = firstName,
                LastName = lastName,
                AssignedAtUtc = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc),
                AssignedBy = AdminId
            };
        }

        private void VerifyNoWrites()
        {
            _staffRepository.Verify(r => r.AddStaffAssignmentAsync(It.IsAny<EventStaffAssignment>()), Times.Never);
            _staffRepository.Verify(r => r.RemoveStaffAssignment(It.IsAny<EventStaffAssignment>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // =====================================================================
        // Assign
        // =====================================================================

        [TestMethod]
        public async Task Assign_EventMissing_ReturnsNotFound_AndDoesNotWrite()
        {
            GivenEventMissing();

            var result = await _service.AssignStaffAsync(EventId, UserId, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Assign_UserMissing_ReturnsNotFound_AndDoesNotWrite()
        {
            GivenUserMissing();

            var result = await _service.AssignStaffAsync(EventId, UserId, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Assign_UserIsNeitherInstructorNorAdmin_ReturnsValidation_AndDoesNotWrite()
        {
            _userManager.Setup(m => m.IsInRoleAsync(_user, Roles.Instructor)).ReturnsAsync(false);
            _userManager.Setup(m => m.IsInRoleAsync(_user, Roles.Admin)).ReturnsAsync(false);

            var result = await _service.AssignStaffAsync(EventId, UserId, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Assign_ChecksTheTargetUsersRoles_NotTheCaller()
        {
            await _service.AssignStaffAsync(EventId, UserId, AdminId);

            _userManager.Verify(m => m.IsInRoleAsync(_user, Roles.Instructor), Times.Once);
        }

        [TestMethod]
        public async Task Assign_Instructor_AddsAssignment_AndSavesOnce()
        {
            var before = DateTime.UtcNow;

            var result = await _service.AssignStaffAsync(EventId, UserId, AdminId);

            var after = DateTime.UtcNow;

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_addedAssignment);
            Assert.AreEqual(EventId, _addedAssignment.EventId);
            Assert.AreEqual(UserId, _addedAssignment.UserId);
            Assert.AreEqual(AdminId, _addedAssignment.AssignedBy);
            Assert.IsTrue(_addedAssignment.AssignedAtUtc >= before && _addedAssignment.AssignedAtUtc <= after,
                "AssignedAtUtc should be the current UTC time.");
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Assign_AdminRoleUser_IsAllowed()
        {
            _userManager.Setup(m => m.IsInRoleAsync(_user, Roles.Instructor)).ReturnsAsync(false);
            _userManager.Setup(m => m.IsInRoleAsync(_user, Roles.Admin)).ReturnsAsync(true);

            var result = await _service.AssignStaffAsync(EventId, UserId, AdminId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_addedAssignment);
        }

        [TestMethod]
        public async Task Assign_AlreadyAssigned_ReturnsSuccess_AndDoesNotWrite()
        {
            // Regression: assigning twice used to hit the unique index (500)
            GivenAlreadyAssigned();

            var result = await _service.AssignStaffAsync(EventId, UserId, AdminId);

            Assert.IsTrue(result.Succeeded);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Assign_ReturnsTheEventsStaffList_WithNames()
        {
            _staffList = [StaffMember(UserId, "Kyle", "Test")];

            var result = await _service.AssignStaffAsync(EventId, UserId, AdminId);

            Assert.AreEqual(1, result.Data!.Count);
            Assert.AreEqual(UserId, result.Data[0].UserId);
            Assert.AreEqual("Kyle", result.Data[0].FirstName);
            Assert.AreEqual("Test", result.Data[0].LastName);
            Assert.AreEqual(EventId, result.Data[0].EventId);
        }

        // =====================================================================
        // Unassign
        // =====================================================================

        [TestMethod]
        public async Task Unassign_EventMissing_ReturnsNotFound_AndDoesNotWrite()
        {
            GivenEventMissing();

            var result = await _service.UnassignStaffAsync(EventId, UserId, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Unassign_UserMissing_ReturnsNotFound_AndDoesNotWrite()
        {
            GivenUserMissing();

            var result = await _service.UnassignStaffAsync(EventId, UserId, AdminId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Unassign_NotAssigned_ReturnsSuccess_AndDoesNotWrite()
        {
            // Regression: Remove(null) used to throw (500)
            var result = await _service.UnassignStaffAsync(EventId, UserId, AdminId);

            Assert.IsTrue(result.Succeeded);
            VerifyNoWrites();
        }

        [TestMethod]
        public async Task Unassign_Assigned_RemovesThatAssignment_AndSavesOnce()
        {
            var assignment = GivenAlreadyAssigned();

            var result = await _service.UnassignStaffAsync(EventId, UserId, AdminId);

            Assert.IsTrue(result.Succeeded);
            _staffRepository.Verify(r => r.RemoveStaffAssignment(assignment), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Unassign_Assigned_ReturnsStaffList_LoadedAfterTheSave()
        {
            // Regression: the list was loaded before the removal was saved,
            // so the response still included the person just unassigned
            GivenAlreadyAssigned();

            await _service.UnassignStaffAsync(EventId, UserId, AdminId);

            var saveIndex = _callOrder.IndexOf("Save");
            var lastLoadIndex = _callOrder.LastIndexOf("LoadStaff");
            Assert.IsTrue(saveIndex >= 0, "Expected a save.");
            Assert.IsTrue(lastLoadIndex > saveIndex,
                $"The staff list must be loaded after the save. Call order: {string.Join(", ", _callOrder)}");
        }

        // =====================================================================
        // Get staff
        // =====================================================================

        [TestMethod]
        public async Task GetStaff_EventMissing_ReturnsNotFound()
        {
            GivenEventMissing();

            var result = await _service.GetEventStaffAsync(EventId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task GetStaff_NoStaff_ReturnsSuccess_WithEmptyList()
        {
            var result = await _service.GetEventStaffAsync(EventId);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Data!.Count);
        }

        [TestMethod]
        public async Task GetStaff_MapsEveryField()
        {
            var otherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            _staffList =
            [
                StaffMember(UserId, "Kyle", "Test"),
                StaffMember(otherUserId, "Ada", "Lovelace")
            ];

            var result = await _service.GetEventStaffAsync(EventId);

            Assert.AreEqual(2, result.Data!.Count);
            var ada = result.Data.Single(s => s.UserId == otherUserId);
            Assert.AreEqual("Ada", ada.FirstName);
            Assert.AreEqual("Lovelace", ada.LastName);
            Assert.AreEqual(EventId, ada.EventId);
            Assert.AreEqual(AdminId, ada.AssignedBy);
            Assert.AreEqual(new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc), ada.AssignedAtUtc);
        }
    }
}