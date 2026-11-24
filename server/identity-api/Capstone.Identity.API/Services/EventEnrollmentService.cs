using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Models.ReadModels;
using Capstone.Identity.API.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Capstone.Identity.API.Services
{
    public class EventEnrollmentService : IEventEnrollmentService
    {
        private const int MaxRosterResults = 200;

        private readonly IEventRepository _eventRepository;
        private readonly IEventEnrollmentRepository _enrollmentRepository;
        private readonly IEventStaffRepository _staffRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IApplicationUserService _applicationUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<EventEnrollmentService> _logger;

        public EventEnrollmentService(
            IEventRepository eventRepository,
            IEventEnrollmentRepository enrollmentRepository,
            IEventStaffRepository staffRepository,
            IUserProfileRepository userProfileRepository,
            IApplicationUserService applicationUserService,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider,
            ILogger<EventEnrollmentService> logger)
        {
            _eventRepository = eventRepository;
            _enrollmentRepository = enrollmentRepository;
            _staffRepository = staffRepository;
            _userProfileRepository = userProfileRepository;
            _applicationUserService = applicationUserService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Result<RegistrantLookupDto>> LookupRegistrantAsync(
            int eventId, string? email, Guid callerUserId, bool callerIsAdmin)
        {
            var access = await GetManageableEventAsync(eventId, callerUserId, callerIsAdmin);
            if (!access.Succeeded)
            {
                return Result<RegistrantLookupDto>.Failure(access.Error!, access.ErrorType);
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return Result<RegistrantLookupDto>.Failure(
                    "An email is required to look up a registrant.", ResultErrorType.Validation);
            }

            // FindByEmailAsync compares the normalized (uppercased) email, so case doesn't matter
            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user is null)
            {
                return Result<RegistrantLookupDto>.Success(new RegistrantLookupDto { Found = false });
            }

            var profile = await _userProfileRepository.GetUserProfileByUserIdAsync(user.Id);
            if (profile is null)
            {
                _logger.LogWarning("User {UserId} has no profile.", user.Id);
                return Result<RegistrantLookupDto>.Failure(
                    "This account is missing its profile.", ResultErrorType.Unexpected);
            }

            var alreadyEnrolled = await _enrollmentRepository.IsEnrolledAsync(eventId, profile.UserProfileId);

            return Result<RegistrantLookupDto>.Success(new RegistrantLookupDto
            {
                Found = true,
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = profile.DisplayName,
                AccountStatus = GetAccountStatus(user.Email, user.PasswordHash != null),
                AlreadyEnrolled = alreadyEnrolled
            });
        }


        public async Task<Result<RosterEntryDto>> EnrollExistingAsync(
            int eventId, Guid userId, Guid callerUserId, bool callerIsAdmin)
        {
            var access = await GetManageableEventAsync(eventId, callerUserId, callerIsAdmin);
            if (!access.Succeeded)
            {
                return Result<RosterEntryDto>.Failure(access.Error!, access.ErrorType);
            }

            var targetEvent = access.Data!;

            var blocker = GetEnrollmentBlocker(targetEvent);
            if (blocker is not null)
            {
                return Result<RosterEntryDto>.Failure(blocker, ResultErrorType.Validation);
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result<RosterEntryDto>.Failure("User not found.", ResultErrorType.NotFound);
            }

            var profile = await _userProfileRepository.GetUserProfileByUserIdAsync(userId);
            if (profile is null)
            {
                _logger.LogWarning("User {UserId} has no profile.", userId);
                return Result<RosterEntryDto>.Failure(
                    "This account is missing its profile.", ResultErrorType.Unexpected);
            }

            // Already enrolled: success, nothing to change
            var existing = await _enrollmentRepository.GetEnrollmentForUpdateAsync(eventId, profile.UserProfileId);
            if (existing is not null)
            {
                return Result<RosterEntryDto>.Success(ToRosterEntryDto(user, profile, existing.EnrolledAtUtc));
            }

            var overlaps = await _enrollmentRepository.HasOverlappingEnrollmentAsync(
                profile.UserProfileId, targetEvent.EventStart, targetEvent.EventEnd, eventId);
            if (overlaps)
            {
                return Result<RosterEntryDto>.Failure(
                    "This participant is already enrolled in another event at the same time.",
                    ResultErrorType.Conflict);
            }

            var enrollment = await AddEnrollmentAsync(eventId, profile.UserProfileId, callerUserId);

            return Result<RosterEntryDto>.Success(ToRosterEntryDto(user, profile, enrollment.EnrolledAtUtc));
        }

        // no account - no placeholder yet
        public async Task<Result<RosterEntryDto>> EnrollNewRegistrantAsync(
            int eventId, NewRegistrantRequestDto request, Guid callerUserId, bool callerIsAdmin)
        {
            var access = await GetManageableEventAsync(eventId, callerUserId, callerIsAdmin);
            if (!access.Succeeded)
            {
                return Result<RosterEntryDto>.Failure(access.Error!, access.ErrorType);
            }

            var blocker = GetEnrollmentBlocker(access.Data!);
            if (blocker is not null)
            {
                return Result<RosterEntryDto>.Failure(blocker, ResultErrorType.Validation);
            }

            var firstName = request.FirstName.Trim();
            var lastName = request.LastName.Trim();

            string? email = null;
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                email = request.Email.Trim();
            }

            // Never link silently: if the email has an account, staff must look it up and confirm
            if (email is not null)
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser is not null)
                {
                    return Result<RosterEntryDto>.Failure(
                        "An account with this email already exists. Look it up and enroll that account instead.",
                        ResultErrorType.Conflict);
                }
            }

            // All checks done. Create the placeholder (no password - PendingActivation), then enroll it.
            var createResult = await _applicationUserService.CreateUserWithProfileAsync(email, null, firstName, lastName);
            if (!createResult.Succeeded)
            {
                return Result<RosterEntryDto>.Failure(createResult.Error!, createResult.ErrorType);
            }

            var newUser = createResult.Data!;

            var profile = await _userProfileRepository.GetUserProfileByUserIdAsync(newUser.Id);
            if (profile is null)
            {
                _logger.LogError("Placeholder {UserId} was created without a profile.", newUser.Id);
                return Result<RosterEntryDto>.Failure(
                    "The registrant was created but could not be enrolled.", ResultErrorType.Unexpected);
            }

            var enrollment = await AddEnrollmentAsync(eventId, profile.UserProfileId, callerUserId);

            return Result<RosterEntryDto>.Success(ToRosterEntryDto(newUser, profile, enrollment.EnrolledAtUtc));
        }


        public async Task<Result<bool>> UnenrollAsync(
            int eventId, Guid userId, Guid callerUserId, bool callerIsAdmin)
        {
            var access = await GetManageableEventAsync(eventId, callerUserId, callerIsAdmin);
            if (!access.Succeeded)
            {
                return Result<bool>.Failure(access.Error!, access.ErrorType);
            }

            var profile = await _userProfileRepository.GetUserProfileByUserIdAsync(userId);
            if (profile is null)
            {
                return Result<bool>.Failure("Participant not found.", ResultErrorType.NotFound);
            }

            var enrollment = await _enrollmentRepository.GetEnrollmentForUpdateAsync(eventId, profile.UserProfileId);

            // Not enrolled: success, nothing to change
            if (enrollment is null)
            {
                return Result<bool>.Success(true);
            }

            _enrollmentRepository.RemoveEnrollment(enrollment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} unenrolled from event {EventId} by {ActingUserId}",
                userId, eventId, callerUserId);

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<RosterEntryDto>>> GetRosterAsync(
            int eventId, string? search, Guid callerUserId, bool callerIsAdmin)
        {
            var access = await GetManageableEventAsync(eventId, callerUserId, callerIsAdmin);
            if (!access.Succeeded)
            {
                return Result<List<RosterEntryDto>>.Failure(access.Error!, access.ErrorType);
            }

            string? term = null;
            if (!string.IsNullOrWhiteSpace(search))
            {
                term = search.Trim();
            }

            var entries = await _enrollmentRepository.GetRosterAsync(eventId, term, MaxRosterResults);

            var dtos = new List<RosterEntryDto>();
            foreach (var entry in entries)
            {
                dtos.Add(ToRosterEntryDto(entry));
            }

            return Result<List<RosterEntryDto>>.Success(dtos);
        }

        // The event must exist, and the caller must be an admin or assigned to it.
        // Both failures are 404 so instructors can't access events they aren't on
        private async Task<Result<Event>> GetManageableEventAsync(int eventId, Guid callerUserId, bool callerIsAdmin)
        {
            var targetEvent = await _eventRepository.GetEventByIdAsync(eventId);
            if (targetEvent is null)
            {
                return Result<Event>.Failure("No event found.", ResultErrorType.NotFound);
            }

            if (!callerIsAdmin)
            {
                var isAssigned = await _staffRepository.IsAssignedAsync(eventId, callerUserId);
                if (!isAssigned)
                {
                    return Result<Event>.Failure("No event found.", ResultErrorType.NotFound);
                }
            }

            return Result<Event>.Success(targetEvent);
        }

        // Returns why nobody can be enrolled in this event right now, or null if they can
        private string? GetEnrollmentBlocker(Event targetEvent)
        {
            if (targetEvent.IsCancelled)
            {
                return "This event has been cancelled.";
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (targetEvent.EventEnd <= now)
            {
                return "This event has already ended.";
            }

            return null;
        }

        private async Task<EventEnrollment> AddEnrollmentAsync(int eventId, int userProfileId, Guid enrolledById)
        {
            var enrollment = new EventEnrollment
            {
                EventId = eventId,
                UserProfileId = userProfileId,
                EnrolledAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                EnrolledById = enrolledById
            };

            _enrollmentRepository.AddEnrollment(enrollment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Profile {UserProfileId} enrolled in event {EventId} by {ActingUserId}",
                userProfileId, eventId, enrolledById);

            return enrollment;
        }

        private static string GetAccountStatus(string? email, bool hasPassword)
        {
            if (email is null)
            {
                return "WalkUp";
            }

            if (!hasPassword)
            {
                return "PendingActivation";
            }

            return "Active";
        }

        private static RosterEntryDto ToRosterEntryDto(ApplicationUser user, UserProfile profile, DateTime enrolledAtUtc)
        {
            return new RosterEntryDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = profile.DisplayName,
                Email = user.Email,
                AccountStatus = GetAccountStatus(user.Email, user.PasswordHash != null),
                EnrolledAtUtc = enrolledAtUtc
            };
        }

        private static RosterEntryDto ToRosterEntryDto(RosterEntry entry)
        {
            return new RosterEntryDto
            {
                UserId = entry.UserId,
                FirstName = entry.FirstName,
                LastName = entry.LastName,
                DisplayName = entry.DisplayName,
                Email = entry.Email,
                AccountStatus = GetAccountStatus(entry.Email, entry.HasPassword),
                EnrolledAtUtc = entry.EnrolledAtUtc
            };
        }
    }
}