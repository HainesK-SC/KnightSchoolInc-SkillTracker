using Capstone.Identity.API.Services;
using Capstone.Identity.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Capstone.Identity.API.Dtos.Request;

namespace Capstone.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, Instructor")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet("{eventId:int}")]
        public async Task<IActionResult> GetEventByIdAsync(int eventId)
        {
            var eventResult = await _eventService.GetEventAsync(eventId);

            if (eventResult.Succeeded == false)
            {
                return eventResult.ToProblemResult();
            }

            var eventById = eventResult.Data;

            return Ok(eventById);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddEventAsync(CreateEventDto newEventDto)
        {
            var addEventResult = await _eventService.CreateNewEventAsync(newEventDto);
            
            if (addEventResult.Succeeded == false)
            {
                return addEventResult.ToProblemResult();
            }

            var eventDto = addEventResult.Data;

            return Ok(eventDto);
        }

        [HttpPut("{eventId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEventAsync(int eventId, UpdateEventDto updateEventDto)
        {
            var updateResult = await _eventService.UpdateEventAsync(eventId, updateEventDto);

            if (updateResult.Succeeded == false)
            {
                return updateResult.ToProblemResult();
            }

            var eventDto = updateResult.Data;

            return Ok(eventDto);
        }

        [HttpDelete("{eventId:int")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEventAsync(int eventId)
        {
            var deleteResult = await _eventService.DeleteEvent(eventId);

            if (deleteResult.Succeeded == false)
            {
                return deleteResult.ToProblemResult();
            }

            return NoContent();
        }

        [HttpPut("{eventId:int}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelEventAsync(int eventId)
        {
            var cancelResult = await _eventService.CancelEventAsync(eventId);

            if (cancelResult.Succeeded == false)
            {
                return cancelResult.ToProblemResult();
            }

            var eventDto = cancelResult.Data;

            return Ok(eventDto);
        }
    }
}
