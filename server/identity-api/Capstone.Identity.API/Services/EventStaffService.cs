using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Mappings;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Capstone.Identity.API.Services
{
    public class EventStaffService : IEventStaffService
    {
        private readonly IEventStaffRepository _eventStaffRepo;
        private readonly IEventRepository _eventRepository;
        private readonly IUserProfileRepository _userProfileRepo;
        private readonly ILogger<EventStaffService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventStaffService(
            IEventStaffRepository eventStaffRepo, 
            IEventRepository eventRepository,
            IUserProfileRepository userProfileRepo,
            ILogger<EventStaffService> logger,
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _eventStaffRepo = eventStaffRepo;
            _eventRepository = eventRepository;
            _userProfileRepo = userProfileRepo;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Result<List<EventStaffDto>>> AssignStaffAsync(int eventId, Guid userId, Guid actingAdminId)
        {
            var eventForUdpate = await _eventRepository.GetEventByIdAsync(eventId);

            if (eventForUdpate == null)
            {
                _logger.LogInformation("No event found with ID: {EventId}", eventId);
                return Result<List<EventStaffDto>>.Failure("No event found.", ResultErrorType.NotFound);
            }
            

            if (_userManager.FindByIdAsync(userId.ToString()) == null)
            {
                _logger.LogInformation("No user found with ID: {UserId}", userId);
                return Result<List<EventStaffDto>>.Failure("The instructor being passed cannot be found.", ResultErrorType.NotFound);
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result<List<EventStaffDto>>.Failure("User not found.", ResultErrorType.NotFound);
            }

            var isInstructor = await _userManager.IsInRoleAsync(user, Roles.Instructor);
            var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);

            if (!isInstructor && !isAdmin)
            {
                return Result<List<EventStaffDto>>.Failure(
                    "Only instructors or admins can be assigned to an event.", ResultErrorType.Validation);
            }

            var newStaffAssigment = new EventStaffAssignment
            {
                EventId = eventId,
                UserId = userId,
                AssignedAtUtc = DateTime.UtcNow,
                AssignedBy = actingAdminId
            };

            await _eventStaffRepo.AddStaffAssignmentAsync(newStaffAssigment);

            await _unitOfWork.SaveChangesAsync();

            var eventStaffDtos = await GetEventStaffAsync(eventId);

            return Result<List<EventStaffDto>>.Success(eventStaffDtos.Data);
        }

        public async Task<Result<List<EventStaffDto>>> UnassignStaffAsync(int eventId, Guid userId, Guid actingAdminId)
        {
            var eventForUdpate = await _eventRepository.GetEventByIdAsync(eventId);

            if (eventForUdpate == null)
            {
                _logger.LogInformation("No event found with ID: {EventId}", eventId);
                return Result<List<EventStaffDto>>.Failure("No event found.", ResultErrorType.NotFound);
            }
            

            if (await _userManager.FindByIdAsync(userId.ToString()) == null)
            {
                _logger.LogInformation("No user found with ID: {UserId}", userId);
                return Result<List<EventStaffDto>>.Failure("The instructor being passed cannot be found.", ResultErrorType.NotFound);
            }

            var assignmentToRemove = await _eventStaffRepo.GetEventStaffAssignmentForUpdateAsync(eventId, userId);

            _eventStaffRepo.RemoveStaffAssignment(assignmentToRemove);

            await _unitOfWork.SaveChangesAsync();

            var eventStaffDtos = await GetEventStaffAsync(eventId);

            return Result<List<EventStaffDto>>.Success(eventStaffDtos.Data);
        }

        public async Task<Result<List<EventStaffDto>>> GetEventStaffAsync(int eventId)
        {
            var eventStaff = await _eventStaffRepo.GetEventStaffForEventAsync(eventId);
            var eventStaffDtos = new List<EventStaffDto>();
            foreach (var e in eventStaff)
            {
                eventStaffDtos.Add(e.ToEventStaffDto());
            }

            return Result<List<EventStaffDto>>.Success(eventStaffDtos);
        }
    }
}
