using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Mappings;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;

namespace Capstone.Identity.API.Services
{
    public class EventService
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

        public async Task<Result<bool>> CancelEvent(int eventId);
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
        public async Task<Result<bool>> DeleteEvent(int eventId);
        public async Task<Result<EventDto>> GetEventAsync(int eventId);
        public async Task<Result<EventDto>> UpdateEventAsync(int eventId);
    }
}