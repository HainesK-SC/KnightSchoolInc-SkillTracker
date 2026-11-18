using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Request;

namespace Capstone.Identity.API.Services
{
    public interface IEventService
    {
        Task<Result<EventDto>> CreateNewEventAsync(CreateEventDto newEvent);
        Task<Result<EventDto>> GetEventAsync(int eventId);
        Task<Result<EventDto>> UpdateEventAsync(int eventId, UpdateEventDto updateEventDto);
        Task<Result<EventDto>> CancelEventAsync(int eventId);
        Task<Result<bool>> DeleteEvent(int eventId);
        Task<Result<IReadOnlyList<EventDto>>> SearchEventsAsync(
            EventSearchRequestDto request,
            Guid callerUserId,
            bool callerIsAdmin);
    }
}
