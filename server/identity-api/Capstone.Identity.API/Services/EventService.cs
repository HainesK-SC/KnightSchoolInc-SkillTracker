using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Mappings;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using System.Reflection.Metadata.Ecma335;

namespace Capstone.Identity.API.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepo;
        private readonly ILogger<EventService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public EventService(
            IEventRepository eventRepository, 
            ILogger<EventService> logger,
            IUnitOfWork unitOfWork)
        {
            _eventRepo = eventRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<EventDto>> CancelEventAsync(int eventId)
        {
            var eventResult = await _eventRepo.GetEventByIdAsync(eventId);

            if (eventResult == null)
            {
                _logger.LogInformation("No event found with ID: {EventId}", eventId);
                return Result<EventDto>.Failure("No event found.", ResultErrorType.NotFound);
            }

            var eventDto = eventResult.ToEventDto();

            if (eventResult.IsCancelled == false)
            {
                eventResult.IsCancelled = true;
                return Result<EventDto>.Success(eventDto);
            }
            else
            {
                _logger.LogInformation("Event is already cancelled. No changes were made.");
                return Result<EventDto>.Success(eventDto); // if it is already cancelled, the request should still return the DTO
            }
        }


        public async Task<Result<EventDto>> CreateNewEventAsync(CreateEventDto newEventDto)
        {
            // checking if the incoming dto is valid
            if (newEventDto == null)
            {
                _logger.LogError("Incoming data transfer object is null.");
                return Result<EventDto>.Failure("An error occurred. Please try again.", ResultErrorType.Unexpected);
            }

            // commented this out for now - I think this is a valid scenario
            // there could be multiple of the same event, planned on different dates  (recurring)
            // checking if an event with the same name exists
            //var eventName = newEventDto.EventName.Trim().ToLower();
            //var isEventExists = _eventRepo.GetEventByNameAsync(eventName);
            //if (isEventExists != null)
            //{
            //    _logger.LogInformation("An event already exists with this name.");
            //    return Result<EventDto>.Failure("Naming conflict. An event with this name already exists.", ResultErrorType.Conflict);
            //}
            
            var newEvent = new Event
            {
                EventName = newEventDto.EventName,
                EventLocation = newEventDto.EventLocation,
                Description = newEventDto.Description,
                EventStart = newEventDto.EventStart,
                EventEnd = newEventDto.EventEnd,
                RegistrationType = newEventDto.RegistrationType
            };

            var addResult = await _eventRepo.AddEventAsync(newEvent);
    
            await _unitOfWork.SaveChangesAsync();

            var eventDto = newEvent.ToEventDto();

            return Result<EventDto>.Success(eventDto);
        }

        // this is a nuclear option, made available for exceptional cases
        // ordinarily, an event will not be deleted from the system but archived
        public async Task<Result<bool>> DeleteEvent(int eventId)
        {
            var eventResult = await _eventRepo.GetEventByIdAsync(eventId);

            if (eventResult == null)
            {
                _logger.LogInformation("No Event found with ID: {EventId}", eventId);
                return Result<bool>.Failure("No event found.", ResultErrorType.NotFound);
            }

            _eventRepo.RemoveEvent(eventResult);

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        public async Task<Result<EventDto>> GetEventAsync(int eventId)
        {
            var getEvent = await _eventRepo.GetEventByIdAsync(eventId);

            if (getEvent == null)
            {
                _logger.LogInformation("No event found with ID: {EventId}.", eventId);
                return Result<EventDto>.Failure("No event found.", ResultErrorType.NotFound);
            }

            var eventDto = getEvent.ToEventDto();

            return Result<EventDto>.Success(eventDto);
        }
        public async Task<Result<EventDto>> UpdateEventAsync(int eventId, UpdateEventDto updateEventDto)
        {
            if (updateEventDto == null)
            {
                _logger.LogError("UpdateEventDto from incoming request is null.");
                return Result<EventDto>.Failure("An unexpected error occurred. Try again.", ResultErrorType.Unexpected);
            }

            var eventForUpdate = await _eventRepo.GetEventByIdForUpdateAsync(eventId);

            if (eventForUpdate == null)
            {
                _logger.LogInformation("No event found with ID: {EventId}", eventId);
                return Result<EventDto>.Failure("No event found.", ResultErrorType.NotFound);
            }

            eventForUpdate.EventName = updateEventDto.EventName.Trim();
            eventForUpdate.Description = updateEventDto.Description.Trim();
            eventForUpdate.EventLocation = updateEventDto.EventLocation.Trim();
            eventForUpdate.EventStart = updateEventDto.EventStart;
            eventForUpdate.EventEnd = updateEventDto.EventEnd;
            eventForUpdate.RegistrationType = updateEventDto.RegistrationType;

            await _unitOfWork.SaveChangesAsync();

            var eventDto = eventForUpdate.ToEventDto();

            return Result<EventDto>.Success(eventDto);
        }
    }
}