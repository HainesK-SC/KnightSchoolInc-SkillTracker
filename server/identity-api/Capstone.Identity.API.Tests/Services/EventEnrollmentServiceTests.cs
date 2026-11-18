using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Models.ReadModels;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Capstone.Identity.API.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class EventEnrollmentServiceTests
    {
        private const int EventId = 5;
        private const int ProfileId = 42;
        private const int NewProfileId = 77;
        private const string Email = "kyle@test.com";

        private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid NewUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        private static readonly Guid AdminId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid InstructorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        private static readonly DateTime EventStartUtc = new(2026, 12, 5, 15, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime EventEndUtc = new(2026, 12, 5, 22, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime EarlierEnrolledAtUtc = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

        private Mock<IEventRepository> _eventRepository = null!;
        private Mock<IEventEnrollmentRepository> _enrollmentRepository = null!;
        private Mock<IEventStaffRepository> _staffRepository = null!;
        private Mock<IUserProfileRepository> _profileRepository = null!;
        private Mock<IApplicationUserService> _applicationUserService = null!;
        private Mock<UserManager<ApplicationUser>> _userManager = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private FakeTimeProvider _timeProvider = null!;
        private EventEnrollmentService _service = null!;

        private Event _event = null!;
        private ApplicationUser _user = null!;
        private ApplicationUser _newUser = null!;
        private EventEnrollment? _addedEnrollment;

        [TestInitialize]
        public void TestInitialize()
        {
            _addedEnrollment = null;

            // Default event: exists, upcoming, not cancelled
            _event = new Event
            {
                EventId = EventId,
                EventName = "Spring Tournament",
                EventLocation = "Toronto",
                EventStart = EventStartUtc,
                EventEnd = EventEndUtc,
                IsCancelled = false
            };
            _eventRepository = new Mock<IEventRepository>();
            _eventRepository.Setup(r => r.GetEventByIdAsync(EventId)).ReturnsAsync(() => _event);

            // Default instructor: assigned to the event
            _staffRepository = new Mock<IEventStaffRepository>();
            _staffRepository.Setup(r => r.IsAssignedAsync(EventId, InstructorId)).ReturnsAsync(true);

            // Default existing user: active account (email + password) with a profile
            _user = new ApplicationUser
            {
                Id = UserId,
                FirstName = "Kyle",
                LastName = "Test",
                Email = Email,
                PasswordHash = "hashed"
            };
            _userManager = IdentityMocks.CreateUserManager();
            _userManager.Setup(m => m.FindByIdAsync(UserId.ToString())).ReturnsAsync(() => _user);
            _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(() => _user);

            _profileRepository = new Mock<IUserProfileRepository>();
            _profileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(UserId))
                .ReturnsAsync(new UserProfile { UserProfileId = ProfileId, ApplicationUserId = UserId, DisplayName = "Knight Kyle the Brave" });
            _profileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(NewUserId))
                .ReturnsAsync(new UserProfile { UserProfileId = NewProfileId, ApplicationUserId = NewUserId, DisplayName = "Knight Ada the Bold" });

            // Default enrollment state: not enrolled, no overlaps
            _enrollmentRepository = new Mock<IEventEnrollmentRepository>();
            _enrollmentRepository
                .Setup(r => r.GetEnrollmentForUpdateAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((EventEnrollment?)null);
            _enrollmentRepository
                .Setup(r => r.IsEnrolledAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            _enrollmentRepository
                .Setup(r => r.HasOverlappingEnrollmentAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            _enrollmentRepository
                .Setup(r => r.GetRosterAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<int>()))
                .ReturnsAsync(new List<RosterEntry>());
            _enrollmentRepository
                .Setup(r => r.AddEnrollment(It.IsAny<EventEnrollment>()))
                .Callback<EventEnrollment>(e => _addedEnrollment = e);

            // Default placeholder creation: succeeds and returns the new user
            _newUser = new ApplicationUser { Id = NewUserId, FirstName = "Ada", LastName = "Lovelace", Email = null, PasswordHash = null };
            _applicationUserService = new Mock<IApplicationUserService>();
            _applicationUserService
                .Setup(s => s.CreateUserWithProfileAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>?>()))
                .ReturnsAsync(() => Result<ApplicationUser>.Success(_newUser));

            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _timeProvider = new FakeTimeProvider(Now);

            _service = new EventEnrollmentService(
                _eventRepository.Object,
                _enrollmentRepository.Object,
                _staffRepository.Object,
                _profileRepository.Object,
                _applicationUserService.Object,
                _userManager.Object,
                _unitOfWork.Object,
                _timeProvider,
                NullLogger<EventEnrollmentService>.Instance);
        }

        // ---------- Helpers ----------

        private void GivenEventMissing()
        {
            _eventRepository.Setup(r => r.GetEventByIdAsync(EventId)).ReturnsAsync((Event?)null);
        }

        private void GivenInstructorNotAssigned()
        {
            _staffRepository.Setup(r => r.IsAssignedAsync(EventId, InstructorId)).ReturnsAsync(false);
        }

        private EventEnrollment GivenAlreadyEnrolled()
        {
            var enrollment = new EventEnrollment
            {
                Id = 9,
                EventId = EventId,
                UserProfileId = ProfileId,
                EnrolledAtUtc = EarlierEnrolledAtUtc,
                EnrolledById = AdminId
            };

            _enrollmentRepository.Setup(r => r.GetEnrollmentForUpdateAsync(EventId, ProfileId)).ReturnsAsync(enrollment);
            _enrollmentRepository.Setup(r => r.IsEnrolledAsync(EventId, ProfileId)).ReturnsAsync(true);

            return enrollment;
        }

        private static NewRegistrantRequestDto NewRegistrant(string first = "Ada", string last = "Lovelace", string? email = null)
        {
            return new NewRegistrantRequestDto { FirstName = first, LastName = last, Email = email };
        }

        private void VerifyNoEnrollmentWrites()
        {
            _enrollmentRepository.Verify(r => r.AddEnrollment(It.IsAny<EventEnrollment>()), Times.Never);
            _enrollmentRepository.Verify(r => r.RemoveEnrollment(It.IsAny<EventEnrollment>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        private void VerifyNoAccountCreated()
        {
            _applicationUserService.Verify(s => s.CreateUserWithProfileAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<string>?>()), Times.Never);
        }

        // =====================================================================
        // Access (shared by every method)
        // =====================================================================

        [TestMethod]
        public async Task Roster_EventMissing_ReturnsNotFound()
        {
            GivenEventMissing();

            var result = await _service.GetRosterAsync(EventId, null, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task Roster_UnassignedInstructor_ReturnsNotFound_AndLoadsNothing()
        {
            GivenInstructorNotAssigned();

            var result = await _service.GetRosterAsync(EventId, null, InstructorId, callerIsAdmin: false);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            _enrollmentRepository.Verify(r => r.GetRosterAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task Roster_AssignedInstructor_Succeeds()
        {
            var result = await _service.GetRosterAsync(EventId, null, InstructorId, callerIsAdmin: false);

            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public async Task Roster_Admin_SkipsAssignmentCheck()
        {
            await _service.GetRosterAsync(EventId, null, AdminId, callerIsAdmin: true);

            _staffRepository.Verify(r => r.IsAssignedAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task Lookup_UnassignedInstructor_ReturnsNotFound()
        {
            GivenInstructorNotAssigned();

            var result = await _service.LookupRegistrantAsync(EventId, Email, InstructorId, callerIsAdmin: false);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task EnrollExisting_UnassignedInstructor_ReturnsNotFound_AndDoesNotWrite()
        {
            GivenInstructorNotAssigned();

            var result = await _service.EnrollExistingAsync(EventId, UserId, InstructorId, callerIsAdmin: false);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task EnrollNew_UnassignedInstructor_ReturnsNotFound_AndCreatesNoAccount()
        {
            GivenInstructorNotAssigned();

            var result = await _service.EnrollNewRegistrantAsync(EventId, NewRegistrant(), InstructorId, callerIsAdmin: false);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoAccountCreated();
        }

        [TestMethod]
        public async Task Unenroll_UnassignedInstructor_ReturnsNotFound_AndDoesNotWrite()
        {
            GivenInstructorNotAssigned();
            GivenAlreadyEnrolled();

            var result = await _service.UnenrollAsync(EventId, UserId, InstructorId, callerIsAdmin: false);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoEnrollmentWrites();
        }

        // =====================================================================
        // Lookup
        // =====================================================================

        [TestMethod]
        [DataRow((string?)null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Lookup_BlankEmail_ReturnsValidation(string? email)
        {
            var result = await _service.LookupRegistrantAsync(EventId, email, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
        }

        [TestMethod]
        public async Task Lookup_NoAccount_ReturnsSuccess_WithFoundFalse()
        {
            var result = await _service.LookupRegistrantAsync(EventId, "nobody@test.com", AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.Data!.Found);
            Assert.IsNull(result.Data.UserId);
        }

        [TestMethod]
        public async Task Lookup_TrimsEmail()
        {
            await _service.LookupRegistrantAsync(EventId, "  kyle@test.com  ", AdminId, callerIsAdmin: true);

            _userManager.Verify(m => m.FindByEmailAsync(Email), Times.Once);
        }

        [TestMethod]
        public async Task Lookup_Found_ReturnsPreview()
        {
            var result = await _service.LookupRegistrantAsync(EventId, Email, AdminId, callerIsAdmin: true);

            var dto = result.Data!;
            Assert.IsTrue(dto.Found);
            Assert.AreEqual(UserId, dto.UserId);
            Assert.AreEqual("Kyle", dto.FirstName);
            Assert.AreEqual("Test", dto.LastName);
            Assert.AreEqual("Knight Kyle the Brave", dto.DisplayName);
            Assert.AreEqual("Active", dto.AccountStatus);
            Assert.IsFalse(dto.AlreadyEnrolled);
        }

        [TestMethod]
        public async Task Lookup_Found_AlreadyEnrolled_SaysSo()
        {
            GivenAlreadyEnrolled();

            var result = await _service.LookupRegistrantAsync(EventId, Email, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Data!.AlreadyEnrolled);
        }

        [TestMethod]
        public async Task Lookup_Found_PendingActivationAccount()
        {
            _user.PasswordHash = null;

            var result = await _service.LookupRegistrantAsync(EventId, Email, AdminId, callerIsAdmin: true);

            Assert.AreEqual("PendingActivation", result.Data!.AccountStatus);
        }

        [TestMethod]
        public async Task Lookup_AccountWithoutProfile_ReturnsUnexpected()
        {
            _profileRepository.Setup(r => r.GetUserProfileByUserIdAsync(UserId)).ReturnsAsync((UserProfile?)null);

            var result = await _service.LookupRegistrantAsync(EventId, Email, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.Unexpected, result.ErrorType);
        }

        // =====================================================================
        // Enroll existing
        // =====================================================================

        [TestMethod]
        public async Task EnrollExisting_CancelledEvent_ReturnsValidation_AndDoesNotWrite()
        {
            _event.IsCancelled = true;

            var result = await _service.EnrollExistingAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task EnrollExisting_EndedEvent_ReturnsValidation_AndDoesNotWrite()
        {
            _event.EventStart = new DateTime(2026, 11, 1, 15, 0, 0, DateTimeKind.Utc);
            _event.EventEnd = new DateTime(2026, 11, 1, 22, 0, 0, DateTimeKind.Utc);

            var result = await _service.EnrollExistingAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task EnrollExisting_EventInProgress_IsAllowed()
        {
            // Started before "now", ends after it: walk-ups at the door
            _event.EventStart = new DateTime(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc);
            _event.EventEnd = new DateTime(2026, 11, 18, 18, 0, 0, DateTimeKind.Utc);

            var result = await _service.EnrollExistingAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public async Task EnrollExisting_UserMissing_ReturnsNotFound_AndDoesNotWrite()
        {
            _userManager.Setup(m => m.FindByIdAsync(UserId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.EnrollExistingAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task EnrollExisting_AlreadyEnrolled_ReturnsExistingEntry_AndDoesNotWrite()
        {
            GivenAlreadyEnrolled();

            var result = await _service.EnrollExistingAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(EarlierEnrolledAtUtc, result.Data!.EnrolledAtUtc);
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task EnrollExisting_OverlappingEnrollment_ReturnsConflict_AndDoesNotWrite()
        {
            _enrollmentRepository
                .Setup(r => r.HasOverlappingEnrollmentAsync(ProfileId, EventStartUtc, EventEndUtc, EventId))
                .ReturnsAsync(true);

            var result = await _service.EnrollExistingAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task EnrollExisting_ChecksOverlap_WithThisEventsDates_ExcludingThisEvent()
        {
            await _service.EnrollExistingAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            _enrollmentRepository.Verify(r => r.HasOverlappingEnrollmentAsync(
                ProfileId, EventStartUtc, EventEndUtc, EventId), Times.Once);
        }

        [TestMethod]
        public async Task EnrollExisting_Success_AddsEnrollment_AndSavesOnce()
        {
            var result = await _service.EnrollExistingAsync(EventId, UserId, InstructorId, callerIsAdmin: false);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_addedEnrollment);
            Assert.AreEqual(EventId, _addedEnrollment.EventId);
            Assert.AreEqual(ProfileId, _addedEnrollment.UserProfileId);
            Assert.AreEqual(Now.UtcDateTime, _addedEnrollment.EnrolledAtUtc);
            Assert.AreEqual(InstructorId, _addedEnrollment.EnrolledById);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task EnrollExisting_Success_ReturnsRosterEntry()
        {
            var result = await _service.EnrollExistingAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            var dto = result.Data!;
            Assert.AreEqual(UserId, dto.UserId);
            Assert.AreEqual("Kyle", dto.FirstName);
            Assert.AreEqual("Knight Kyle the Brave", dto.DisplayName);
            Assert.AreEqual(Email, dto.Email);
            Assert.AreEqual("Active", dto.AccountStatus);
            Assert.AreEqual(Now.UtcDateTime, dto.EnrolledAtUtc);
        }

        // =====================================================================
        // Enroll new registrant
        // =====================================================================

        [TestMethod]
        public async Task EnrollNew_CancelledEvent_ReturnsValidation_AndCreatesNoAccount()
        {
            _event.IsCancelled = true;

            var result = await _service.EnrollNewRegistrantAsync(EventId, NewRegistrant(), AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNoAccountCreated();
        }

        [TestMethod]
        public async Task EnrollNew_EmailBelongsToExistingAccount_ReturnsConflict_AndCreatesNoAccount()
        {
            var result = await _service.EnrollNewRegistrantAsync(
                EventId, NewRegistrant(email: Email), AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            VerifyNoAccountCreated();
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task EnrollNew_CreatesPlaceholder_WithNoPassword_AndTrimmedValues()
        {
            await _service.EnrollNewRegistrantAsync(
                EventId, NewRegistrant(first: "  Ada  ", last: "  Lovelace  ", email: "  ada@test.com  "), AdminId, callerIsAdmin: true);

            _applicationUserService.Verify(s => s.CreateUserWithProfileAsync(
                "ada@test.com", null, "Ada", "Lovelace", It.IsAny<IReadOnlyCollection<string>?>()), Times.Once);
        }

        [TestMethod]
        [DataRow((string?)null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task EnrollNew_BlankEmail_CreatesPlaceholderWithNullEmail(string? email)
        {
            await _service.EnrollNewRegistrantAsync(EventId, NewRegistrant(email: email), AdminId, callerIsAdmin: true);

            _applicationUserService.Verify(s => s.CreateUserWithProfileAsync(
                null, null, "Ada", "Lovelace", It.IsAny<IReadOnlyCollection<string>?>()), Times.Once);
        }

        [TestMethod]
        public async Task EnrollNew_AccountCreationFails_PassesErrorThrough_AndDoesNotEnroll()
        {
            _applicationUserService
                .Setup(s => s.CreateUserWithProfileAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<string>?>()))
                .ReturnsAsync(Result<ApplicationUser>.Failure("Invalid name.", ResultErrorType.Validation));

            var result = await _service.EnrollNewRegistrantAsync(EventId, NewRegistrant(), AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            Assert.AreEqual("Invalid name.", result.Error);
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task EnrollNew_Success_EnrollsTheNewProfile()
        {
            var result = await _service.EnrollNewRegistrantAsync(EventId, NewRegistrant(), AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_addedEnrollment);
            Assert.AreEqual(EventId, _addedEnrollment.EventId);
            Assert.AreEqual(NewProfileId, _addedEnrollment.UserProfileId);
            Assert.AreEqual(AdminId, _addedEnrollment.EnrolledById);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task EnrollNew_NoEmail_ReturnsWalkUpEntry()
        {
            var result = await _service.EnrollNewRegistrantAsync(EventId, NewRegistrant(), AdminId, callerIsAdmin: true);

            var dto = result.Data!;
            Assert.AreEqual(NewUserId, dto.UserId);
            Assert.AreEqual("Knight Ada the Bold", dto.DisplayName);
            Assert.IsNull(dto.Email);
            Assert.AreEqual("WalkUp", dto.AccountStatus);
        }

        [TestMethod]
        public async Task EnrollNew_WithEmail_ReturnsPendingActivationEntry()
        {
            _newUser.Email = "ada@test.com";

            var result = await _service.EnrollNewRegistrantAsync(
                EventId, NewRegistrant(email: "ada@test.com"), AdminId, callerIsAdmin: true);

            Assert.AreEqual("PendingActivation", result.Data!.AccountStatus);
        }

        // =====================================================================
        // Unenroll
        // =====================================================================

        [TestMethod]
        public async Task Unenroll_ProfileMissing_ReturnsNotFound()
        {
            _profileRepository.Setup(r => r.GetUserProfileByUserIdAsync(UserId)).ReturnsAsync((UserProfile?)null);

            var result = await _service.UnenrollAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task Unenroll_NotEnrolled_ReturnsSuccess_AndDoesNotWrite()
        {
            var result = await _service.UnenrollAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            VerifyNoEnrollmentWrites();
        }

        [TestMethod]
        public async Task Unenroll_Enrolled_RemovesIt_AndSavesOnce()
        {
            var enrollment = GivenAlreadyEnrolled();

            var result = await _service.UnenrollAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            _enrollmentRepository.Verify(r => r.RemoveEnrollment(enrollment), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Unenroll_CancelledEvent_IsStillAllowed()
        {
            _event.IsCancelled = true;
            var enrollment = GivenAlreadyEnrolled();

            var result = await _service.UnenrollAsync(EventId, UserId, AdminId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            _enrollmentRepository.Verify(r => r.RemoveEnrollment(enrollment), Times.Once);
        }

        // =====================================================================
        // Roster
        // =====================================================================

        [TestMethod]
        public async Task Roster_TrimsSearch()
        {
            await _service.GetRosterAsync(EventId, "  kyle  ", AdminId, callerIsAdmin: true);

            _enrollmentRepository.Verify(r => r.GetRosterAsync(EventId, "kyle", It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        [DataRow((string?)null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Roster_BlankSearch_MeansNoFilter(string? search)
        {
            await _service.GetRosterAsync(EventId, search, AdminId, callerIsAdmin: true);

            _enrollmentRepository.Verify(r => r.GetRosterAsync(EventId, null, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public async Task Roster_PassesAResultCap()
        {
            await _service.GetRosterAsync(EventId, null, AdminId, callerIsAdmin: true);

            _enrollmentRepository.Verify(r => r.GetRosterAsync(EventId, null, It.Is<int>(max => max > 0)), Times.Once);
        }

        [TestMethod]
        public async Task Roster_MapsEntries_WithAccountStatus()
        {
            _enrollmentRepository
                .Setup(r => r.GetRosterAsync(EventId, null, It.IsAny<int>()))
                .ReturnsAsync(new List<RosterEntry>
                {
                    new() { UserId = UserId, FirstName = "Kyle", LastName = "Test", DisplayName = "Knight Kyle the Brave", Email = Email, HasPassword = true, EnrolledAtUtc = EarlierEnrolledAtUtc },
                    new() { UserId = NewUserId, FirstName = "Ada", LastName = "Lovelace", DisplayName = "Knight Ada the Bold", Email = null, HasPassword = false, EnrolledAtUtc = EarlierEnrolledAtUtc },
                    new() { UserId = AdminId, FirstName = "Grace", LastName = "Hopper", DisplayName = "Sir Grace the Wise", Email = "grace@test.com", HasPassword = false, EnrolledAtUtc = EarlierEnrolledAtUtc }
                });

            var result = await _service.GetRosterAsync(EventId, null, AdminId, callerIsAdmin: true);

            Assert.AreEqual(3, result.Data!.Count);
            Assert.AreEqual("Active", result.Data.Single(r => r.UserId == UserId).AccountStatus);
            Assert.AreEqual("WalkUp", result.Data.Single(r => r.UserId == NewUserId).AccountStatus);
            Assert.AreEqual("PendingActivation", result.Data.Single(r => r.UserId == AdminId).AccountStatus);
            Assert.AreEqual("Knight Kyle the Brave", result.Data.Single(r => r.UserId == UserId).DisplayName);
        }
    }
}