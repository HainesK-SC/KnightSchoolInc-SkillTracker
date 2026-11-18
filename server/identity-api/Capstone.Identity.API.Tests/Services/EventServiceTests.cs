using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class EventServiceTests
    {
        private const int EventId = 5;

        // 10:00 in Toronto (UTC-05:00) on Dec 5 = 15:00 UTC.
        // .LocalDateTime gives a Local-kind DateTime in whatever time zone the test machine
        // is in; ToUniversalTime() always brings it back to 15:00 UTC, so this is deterministic.
        private static readonly DateTime StartFromOffset =
            new DateTimeOffset(2026, 12, 5, 10, 0, 0, TimeSpan.FromHours(-5)).LocalDateTime;
        private static readonly DateTime EndFromOffset =
            new DateTimeOffset(2026, 12, 5, 17, 0, 0, TimeSpan.FromHours(-5)).LocalDateTime;

        private static readonly DateTime ExpectedStartUtc = new(2026, 12, 5, 15, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ExpectedEndUtc = new(2026, 12, 5, 22, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime OriginalStartUtc = new(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime OriginalEndUtc = new(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc);

        private static readonly Guid CallerUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private Mock<IEventRepository> _eventRepository = null!;
        private Mock<IEventStaffRepository> _staffRepository = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private EventService _service = null!;

        private Event? _addedEvent;
        private List<Event> _searchResults = [];
        private Mock<IEventEnrollmentRepository> _eventEnrollmentRepository;

        [TestInitialize]
        public void TestInitialize()
        {
            _addedEvent = null;
            _searchResults = [];
            _eventEnrollmentRepository = new Mock<IEventEnrollmentRepository>();

            _eventRepository = new Mock<IEventRepository>();
            _eventRepository
                .Setup(r => r.AddEventAsync(It.IsAny<Event>()))
                .Callback<Event>(e => _addedEvent = e)
                .ReturnsAsync((Event e) => e);
            _eventRepository
                .Setup(r => r.SearchEventsAsync(
                    It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<int>()))
                .ReturnsAsync(() => _searchResults);

            // Default: no staff assigned to any event
            _staffRepository = new Mock<IEventStaffRepository>();
            _staffRepository
                .Setup(r => r.AnyForEventAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _service = new EventService(
                _eventRepository.Object,
                NullLogger<EventService>.Instance,
                _unitOfWork.Object,
                _staffRepository.Object,
                _eventEnrollmentRepository.Object);
        }

        // Verifies the arguments the service passed to the repository's search
        private void VerifySearchedWith(
            string? search = null,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            bool includeCancelled = false,
            Guid? staffUserId = null)
        {
            _eventRepository.Verify(r => r.SearchEventsAsync(
                search, fromUtc, toUtc, includeCancelled, staffUserId, It.IsAny<int>()), Times.Once);
        }

        // ---------- Helpers ----------

        // An existing event, returned by both the read-only and the tracked lookups
        private Event GivenEvent(bool isCancelled = false)
        {
            var existing = new Event
            {
                EventId = EventId,
                EventName = "Spring Tournament",
                EventLocation = "Toronto",
                Description = "Original description.",
                EventStart = OriginalStartUtc,
                EventEnd = OriginalEndUtc,
                RegistrationType = EventRegistrationType.PUBLIC,
                IsCancelled = isCancelled
            };

            _eventRepository.Setup(r => r.GetEventByIdAsync(EventId)).ReturnsAsync(existing);
            _eventRepository.Setup(r => r.GetEventByIdForUpdateAsync(EventId)).ReturnsAsync(existing);

            return existing;
        }

        private static CreateEventDto CreateRequest(
            string name = "Spring Tournament",
            string location = "Toronto",
            string? description = "Annual spring event.",
            DateTime? start = null,
            DateTime? end = null,
            EventRegistrationType registrationType = EventRegistrationType.PUBLIC)
        {
            return new CreateEventDto
            {
                EventName = name,
                EventLocation = location,
                Description = description,
                EventStart = start ?? StartFromOffset,
                EventEnd = end ?? EndFromOffset,
                RegistrationType = registrationType
            };
        }

        private static UpdateEventDto UpdateRequest(
            string name = "Spring Tournament 2026",
            string location = "Mississauga",
            string? description = "Moved venues.",
            DateTime? start = null,
            DateTime? end = null,
            EventRegistrationType registrationType = EventRegistrationType.PRIVATE)
        {
            return new UpdateEventDto
            {
                EventName = name,
                EventLocation = location,
                Description = description,
                EventStart = start ?? StartFromOffset,
                EventEnd = end ?? EndFromOffset,
                RegistrationType = registrationType
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
        // Get
        // =====================================================================

        [TestMethod]
        public async Task GetEvent_Found_ReturnsAllFieldsMapped()
        {
            GivenEvent(isCancelled: true);

            var result = await _service.GetEventAsync(EventId);

            Assert.IsTrue(result.Succeeded);
            var dto = result.Data!;
            Assert.AreEqual(EventId, dto.EventId);
            Assert.AreEqual("Spring Tournament", dto.EventName);
            Assert.AreEqual("Toronto", dto.EventLocation);
            Assert.AreEqual("Original description.", dto.Description);
            Assert.AreEqual(OriginalStartUtc, dto.EventStart);
            Assert.AreEqual(OriginalEndUtc, dto.EventEnd);
            Assert.AreEqual(EventRegistrationType.PUBLIC, dto.RegistrationType);
            Assert.IsTrue(dto.IsCancelled);
        }

        [TestMethod]
        public async Task GetEvent_Missing_ReturnsNotFound()
        {
            var result = await _service.GetEventAsync(999);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        // =====================================================================
        // Create
        // =====================================================================

        [TestMethod]
        public async Task Create_Success_AddsOnce_AndSavesOnce()
        {
            var result = await _service.CreateNewEventAsync(CreateRequest());

            Assert.IsTrue(result.Succeeded);
            _eventRepository.Verify(r => r.AddEventAsync(It.IsAny<Event>()), Times.Once);
            VerifySavedOnce();
        }

        [TestMethod]
        public async Task Create_TrimsNameAndLocation()
        {
            await _service.CreateNewEventAsync(CreateRequest(name: "  Spring Tournament  ", location: "  Toronto  "));

            Assert.IsNotNull(_addedEvent);
            Assert.AreEqual("Spring Tournament", _addedEvent.EventName);
            Assert.AreEqual("Toronto", _addedEvent.EventLocation);
        }

        [TestMethod]
        public async Task Create_StoresTrimmedDescription()
        {
            // Regression: a real description was never assigned on create
            var result = await _service.CreateNewEventAsync(CreateRequest(description: "  Annual spring event.  "));

            Assert.IsNotNull(_addedEvent);
            Assert.AreEqual("Annual spring event.", _addedEvent.Description);
            Assert.AreEqual("Annual spring event.", result.Data!.Description);
        }

        [TestMethod]
        [DataRow((string?)null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Create_BlankDescription_StoredAsNull(string? description)
        {
            await _service.CreateNewEventAsync(CreateRequest(description: description));

            Assert.IsNotNull(_addedEvent);
            Assert.IsNull(_addedEvent.Description);
        }

        [TestMethod]
        public async Task Create_ConvertsDatesToUtc()
        {
            await _service.CreateNewEventAsync(CreateRequest());

            Assert.IsNotNull(_addedEvent);
            Assert.AreEqual(ExpectedStartUtc, _addedEvent.EventStart);
            Assert.AreEqual(ExpectedEndUtc, _addedEvent.EventEnd);
            Assert.AreEqual(DateTimeKind.Utc, _addedEvent.EventStart.Kind);
            Assert.AreEqual(DateTimeKind.Utc, _addedEvent.EventEnd.Kind);
        }

        [TestMethod]
        [DataRow(EventRegistrationType.PUBLIC)]
        [DataRow(EventRegistrationType.PRIVATE)]
        public async Task Create_StoresRegistrationType(EventRegistrationType registrationType)
        {
            await _service.CreateNewEventAsync(CreateRequest(registrationType: registrationType));

            Assert.IsNotNull(_addedEvent);
            Assert.AreEqual(registrationType, _addedEvent.RegistrationType);
        }

        [TestMethod]
        public async Task Create_NewEvent_IsNotCancelled()
        {
            var result = await _service.CreateNewEventAsync(CreateRequest());

            Assert.IsNotNull(_addedEvent);
            Assert.IsFalse(_addedEvent.IsCancelled);
            Assert.IsFalse(result.Data!.IsCancelled);
        }

        [TestMethod]
        public async Task Create_EndBeforeStart_ReturnsValidation_AndDoesNotAdd()
        {
            var result = await _service.CreateNewEventAsync(CreateRequest(start: ExpectedEndUtc, end: ExpectedStartUtc));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            _eventRepository.Verify(r => r.AddEventAsync(It.IsAny<Event>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Create_EndEqualToStart_ReturnsValidation_AndDoesNotAdd()
        {
            // Regression: start == end was accepted (zero-length event)
            var result = await _service.CreateNewEventAsync(CreateRequest(start: ExpectedStartUtc, end: ExpectedStartUtc));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            _eventRepository.Verify(r => r.AddEventAsync(It.IsAny<Event>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Create_EndBeforeStart_AcrossTimeZones_IsComparedInUtc()
        {
            // 11:00 at UTC-05:00 is 16:00 UTC; 15:30 UTC is earlier, so the end is before the start
            var start = new DateTimeOffset(2026, 12, 5, 11, 0, 0, TimeSpan.FromHours(-5)).LocalDateTime;
            var end = new DateTime(2026, 12, 5, 15, 30, 0, DateTimeKind.Utc);

            var result = await _service.CreateNewEventAsync(CreateRequest(start: start, end: end));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
        }

        [TestMethod]
        public async Task Create_ReturnsDto_WithStoredValues()
        {
            var result = await _service.CreateNewEventAsync(
                CreateRequest(name: "  Spring Tournament  ", registrationType: EventRegistrationType.PRIVATE));

            var dto = result.Data!;
            Assert.AreEqual("Spring Tournament", dto.EventName);
            Assert.AreEqual(ExpectedStartUtc, dto.EventStart);
            Assert.AreEqual(ExpectedEndUtc, dto.EventEnd);
            Assert.AreEqual(EventRegistrationType.PRIVATE, dto.RegistrationType);
        }

        // =====================================================================
        // Update
        // =====================================================================

        [TestMethod]
        public async Task Update_Missing_ReturnsNotFound_AndDoesNotSave()
        {
            var result = await _service.UpdateEventAsync(999, UpdateRequest());

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Update_Success_UpdatesAllFields_Trimmed_AndSavesOnce()
        {
            var existing = GivenEvent();

            var result = await _service.UpdateEventAsync(EventId, UpdateRequest(
                name: "  Spring Tournament 2026  ",
                location: "  Mississauga  ",
                description: "  Moved venues.  ",
                registrationType: EventRegistrationType.PRIVATE));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("Spring Tournament 2026", existing.EventName);
            Assert.AreEqual("Mississauga", existing.EventLocation);
            Assert.AreEqual("Moved venues.", existing.Description);
            Assert.AreEqual(ExpectedStartUtc, existing.EventStart);
            Assert.AreEqual(ExpectedEndUtc, existing.EventEnd);
            Assert.AreEqual(EventRegistrationType.PRIVATE, existing.RegistrationType);
            VerifySavedOnce();
        }

        [TestMethod]
        [DataRow((string?)null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Update_BlankDescription_ClearsToNull(string? description)
        {
            // Regression ("   "): whitespace was trimmed to "" instead of becoming null
            var existing = GivenEvent();

            await _service.UpdateEventAsync(EventId, UpdateRequest(description: description));

            Assert.IsNull(existing.Description);
        }

        [TestMethod]
        public async Task Update_EndBeforeStart_ReturnsValidation_AndLeavesEventUnchanged()
        {
            var existing = GivenEvent();

            var result = await _service.UpdateEventAsync(EventId,
                UpdateRequest(name: "Changed", start: ExpectedEndUtc, end: ExpectedStartUtc));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            Assert.AreEqual("Spring Tournament", existing.EventName);
            Assert.AreEqual(OriginalStartUtc, existing.EventStart);
            Assert.AreEqual(OriginalEndUtc, existing.EventEnd);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Update_EndEqualToStart_ReturnsValidation_AndDoesNotSave()
        {
            GivenEvent();

            var result = await _service.UpdateEventAsync(EventId,
                UpdateRequest(start: ExpectedStartUtc, end: ExpectedStartUtc));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Update_ConvertsDatesToUtc()
        {
            var existing = GivenEvent();

            await _service.UpdateEventAsync(EventId, UpdateRequest());

            Assert.AreEqual(ExpectedStartUtc, existing.EventStart);
            Assert.AreEqual(DateTimeKind.Utc, existing.EventStart.Kind);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Update_NeverChangesIsCancelled(bool isCancelled)
        {
            var existing = GivenEvent(isCancelled);

            var result = await _service.UpdateEventAsync(EventId, UpdateRequest());

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(isCancelled, existing.IsCancelled);
            Assert.AreEqual(isCancelled, result.Data!.IsCancelled);
        }

        // =====================================================================
        // Cancel
        // =====================================================================

        [TestMethod]
        public async Task Cancel_Missing_ReturnsNotFound_AndDoesNotSave()
        {
            var result = await _service.CancelEventAsync(999);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Cancel_ActiveEvent_SetsFlag_AndSavesOnce()
        {
            var existing = GivenEvent(isCancelled: false);

            var result = await _service.CancelEventAsync(EventId);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(existing.IsCancelled);
            VerifySavedOnce();
        }

        [TestMethod]
        public async Task Cancel_ActiveEvent_UsesTrackedRead()
        {
            // The flag only persists if EF is tracking the entity
            GivenEvent(isCancelled: false);

            await _service.CancelEventAsync(EventId);

            _eventRepository.Verify(r => r.GetEventByIdForUpdateAsync(EventId), Times.Once);
        }

        [TestMethod]
        public async Task Cancel_ActiveEvent_ReturnsDto_WithIsCancelledTrue()
        {
            GivenEvent(isCancelled: false);

            var result = await _service.CancelEventAsync(EventId);

            Assert.IsTrue(result.Data!.IsCancelled);
            Assert.AreEqual(EventId, result.Data.EventId);
        }

        [TestMethod]
        public async Task Cancel_AlreadyCancelled_ReturnsSuccess_AndDoesNotSave()
        {
            GivenEvent(isCancelled: true);

            var result = await _service.CancelEventAsync(EventId);

            Assert.IsTrue(result.Succeeded);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Cancel_AlreadyCancelled_ReturnsTheEvent_NotAnEmptyDto()
        {
            // Regression: the already-cancelled branch returned an unfilled new EventDto()
            GivenEvent(isCancelled: true);

            var result = await _service.CancelEventAsync(EventId);

            var dto = result.Data!;
            Assert.AreEqual(EventId, dto.EventId);
            Assert.AreEqual("Spring Tournament", dto.EventName);
            Assert.IsTrue(dto.IsCancelled);
        }

        // =====================================================================
        // Delete
        // =====================================================================

        [TestMethod]
        public async Task Delete_Missing_ReturnsNotFound_AndRemovesNothing()
        {
            var result = await _service.DeleteEvent(999);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
            _eventRepository.Verify(r => r.RemoveEvent(It.IsAny<Event>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Delete_Found_RemovesEvent_AndSavesOnce(bool isCancelled)
        {
            var existing = GivenEvent(isCancelled);

            var result = await _service.DeleteEvent(EventId);

            Assert.IsTrue(result.Succeeded);
            _eventRepository.Verify(r => r.RemoveEvent(existing), Times.Once);
            VerifySavedOnce();
        }

        [TestMethod]
        public async Task Delete_EventHasStaff_ReturnsConflict_AndRemovesNothing()
        {
            GivenEvent();
            _staffRepository.Setup(r => r.AnyForEventAsync(EventId)).ReturnsAsync(true);

            var result = await _service.DeleteEvent(EventId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            _eventRepository.Verify(r => r.RemoveEvent(It.IsAny<Event>()), Times.Never);
            VerifyNeverSaved();
        }

        [TestMethod]
        public async Task Delete_ChecksStaff_ForThatEvent()
        {
            GivenEvent();

            await _service.DeleteEvent(EventId);

            _staffRepository.Verify(r => r.AnyForEventAsync(EventId), Times.Once);
        }

        // =====================================================================
        // Search
        // =====================================================================

        [TestMethod]
        public async Task Search_Admin_SeesAllEvents_NoStaffFilter()
        {
            await _service.SearchEventsAsync(new EventSearchRequestDto(), CallerUserId, callerIsAdmin: true);

            VerifySearchedWith(staffUserId: null);
        }

        [TestMethod]
        public async Task Search_Instructor_IsLimitedToTheirOwnAssignments()
        {
            // Regression: the staff filter was never set, so instructors saw every event
            await _service.SearchEventsAsync(new EventSearchRequestDto(), CallerUserId, callerIsAdmin: false);

            VerifySearchedWith(staffUserId: CallerUserId);
        }

        [TestMethod]
        public async Task Search_TrimsSearchText()
        {
            await _service.SearchEventsAsync(
                new EventSearchRequestDto { Search = "  toronto  " }, CallerUserId, callerIsAdmin: true);

            VerifySearchedWith(search: "toronto");
        }

        [TestMethod]
        [DataRow((string?)null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Search_BlankSearchText_MeansNoTextFilter(string? search)
        {
            await _service.SearchEventsAsync(
                new EventSearchRequestDto { Search = search }, CallerUserId, callerIsAdmin: true);

            VerifySearchedWith(search: null);
        }

        [TestMethod]
        public async Task Search_ConvertsDatesToUtc()
        {
            await _service.SearchEventsAsync(
                new EventSearchRequestDto { From = StartFromOffset, To = EndFromOffset }, CallerUserId, callerIsAdmin: true);

            VerifySearchedWith(fromUtc: ExpectedStartUtc, toUtc: ExpectedEndUtc);
        }

        [TestMethod]
        public async Task Search_FromAfterTo_ReturnsValidation_AndDoesNotQuery()
        {
            var result = await _service.SearchEventsAsync(
                new EventSearchRequestDto { From = ExpectedEndUtc, To = ExpectedStartUtc }, CallerUserId, callerIsAdmin: true);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            _eventRepository.Verify(r => r.SearchEventsAsync(
                It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task Search_OnlyOneDate_IsAllowed()
        {
            var result = await _service.SearchEventsAsync(
                new EventSearchRequestDto { From = ExpectedStartUtc }, CallerUserId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            VerifySearchedWith(fromUtc: ExpectedStartUtc, toUtc: null);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Search_PassesIncludeCancelled(bool includeCancelled)
        {
            await _service.SearchEventsAsync(
                new EventSearchRequestDto { IncludeCancelled = includeCancelled }, CallerUserId, callerIsAdmin: true);

            VerifySearchedWith(includeCancelled: includeCancelled);
        }

        [TestMethod]
        public async Task Search_AlwaysPassesAResultCap()
        {
            await _service.SearchEventsAsync(new EventSearchRequestDto(), CallerUserId, callerIsAdmin: true);

            _eventRepository.Verify(r => r.SearchEventsAsync(
                It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<bool>(), It.IsAny<Guid?>(), It.Is<int>(max => max > 0)), Times.Once);
        }

        [TestMethod]
        public async Task Search_MapsResults()
        {
            _searchResults =
            [
                new Event { EventId = 1, EventName = "Spring Tournament", EventLocation = "Toronto", EventStart = OriginalStartUtc, EventEnd = OriginalEndUtc },
                new Event { EventId = 2, EventName = "Fall Tournament", EventLocation = "Hamilton", EventStart = OriginalStartUtc, EventEnd = OriginalEndUtc }
            ];

            var result = await _service.SearchEventsAsync(new EventSearchRequestDto(), CallerUserId, callerIsAdmin: true);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(2, result.Data!.Count);
            Assert.AreEqual(1, result.Data[0].EventId);
            Assert.AreEqual("Fall Tournament", result.Data[1].EventName);
        }

        [TestMethod]
        public async Task Search_NoResults_ReturnsSuccess_WithEmptyList()
        {
            var result = await _service.SearchEventsAsync(new EventSearchRequestDto(), CallerUserId, callerIsAdmin: false);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Data!.Count);
        }
    }
}