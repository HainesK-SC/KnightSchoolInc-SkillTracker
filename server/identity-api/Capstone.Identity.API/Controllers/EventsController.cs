using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Admin + "," + Roles.Instructor)]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet("{eventId:int}")]
        public async Task<IActionResult> GetEventById(int eventId)
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
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> AddEvent(CreateEventDto newEventDto)
        {
            var addEventResult = await _eventService.CreateNewEventAsync(newEventDto);

            if (addEventResult.Succeeded == false)
            {
                return addEventResult.ToProblemResult();
            }

            var eventDto = addEventResult.Data;

            return CreatedAtAction(nameof(GetEventById), new { eventId = eventDto.EventId }, eventDto);
        }

        [HttpPut("{eventId:int}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateEvent(int eventId, UpdateEventDto updateEventDto)
        {
            var updateResult = await _eventService.UpdateEventAsync(eventId, updateEventDto);

            if (updateResult.Succeeded == false)
            {
                return updateResult.ToProblemResult();
            }

            var eventDto = updateResult.Data;

            return Ok(eventDto);
        }

        [HttpDelete("{eventId:int}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            var deleteResult = await _eventService.DeleteEvent(eventId);

            if (deleteResult.Succeeded == false)
            {
                return deleteResult.ToProblemResult();
            }

            return NoContent();
        }

        [HttpPut("{eventId:int}/cancel")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> CancelEvent(int eventId)
        {
            var cancelResult = await _eventService.CancelEventAsync(eventId);

            if (cancelResult.Succeeded == false)
            {
                return cancelResult.ToProblemResult();
            }

            var eventDto = cancelResult.Data;

            return Ok(eventDto);
        }

        [HttpGet]
        public async Task<IActionResult> SearchEvents([FromQuery] EventSearchRequestDto request)
        {
            var callerUserId = User.GetUserId();
            var callerIsAdmin = User.IsInRole(Roles.Admin);

            var result = await _eventService.SearchEventsAsync(request, callerUserId, callerIsAdmin);

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }
    }
}
