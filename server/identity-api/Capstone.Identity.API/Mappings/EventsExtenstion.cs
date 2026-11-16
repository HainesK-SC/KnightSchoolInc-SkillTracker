using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Mappings
{
    public static class EventsExtenstion
    {
        public static EventDto ToEventDto(this Event eventToMap)
        {
            var eventDto = new EventDto
            {
                EventId = eventToMap.EventId,
                EventName = eventToMap.EventName,
                EventLocation = eventToMap.EventLocation,
                Description = eventToMap.Description,
                EventStart = eventToMap.EventStart,
                EventEnd = eventToMap.EventEnd,
                RegistrationType = eventToMap.RegistrationType,
                IsCancelled = eventToMap.IsCancelled
    };

            return eventDto;
        }
    }
}
