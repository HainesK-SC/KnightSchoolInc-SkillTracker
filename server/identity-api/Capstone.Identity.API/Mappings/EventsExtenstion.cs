using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Mappings
{
    public class EventsExtenstion
    {
        public EventDto ToEventDto(Event eventToMap)
        {
            var eventDto = new EventDto
            {
                EventName = eventToMap.EventName,
                EventLocation = eventToMap.EventLocation,
                Description = eventToMap.Description,
                EventStart = eventToMap.EventStart,
                EventEnd = eventToMap.EventEnd,
                RegistrationType = eventToMap.RegistrationType
            };

            return eventDto;
        }
    }
}
