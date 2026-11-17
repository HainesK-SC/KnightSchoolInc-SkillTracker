using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Controllers
{
    [Route("api/events/{eventId:int}/staff")]
    [ApiController]
    [Authorize(Roles = Roles.Admin + "," + Roles.Instructor)]
    public class EventStaffController : ControllerBase
    {
        private readonly IEventStaffService _eventStaffService;

        public EventStaffController(IEventStaffService eventStaffService)
        {
            _eventStaffService = eventStaffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEventStaff(int eventId)
        {
            var result = await _eventStaffService.GetEventStaffAsync(eventId);

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        [HttpPost("{userId:guid}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> AssignStaff(int eventId, Guid userId)
        {
            var result = await _eventStaffService.AssignStaffAsync(eventId, userId, User.GetUserId());

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        [HttpDelete("{userId:guid}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UnassignStaff(int eventId, Guid userId)
        {
            var result = await _eventStaffService.UnassignStaffAsync(eventId, userId, User.GetUserId());

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }
    }
}
