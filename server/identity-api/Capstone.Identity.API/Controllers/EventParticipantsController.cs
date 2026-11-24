using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:int}/participants")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Instructor)]
    public class EventParticipantsController : ControllerBase
    {
        private readonly IEventEnrollmentService _enrollmentService;
        private readonly IEventSkillProgressService _eventSkillProgressService;

        public EventParticipantsController(IEventEnrollmentService enrollmentService, IEventSkillProgressService eventSkillProgressService)
        {
            _enrollmentService = enrollmentService;
            _eventSkillProgressService = eventSkillProgressService;
        }

        // GET /api/events/{eventId}/participants?search=kyle
        [HttpGet]
        public async Task<IActionResult> GetRoster(int eventId, [FromQuery] string? search)
        {
            var result = await _enrollmentService.GetRosterAsync(
                eventId, search, User.GetUserId(), User.IsInRole(Roles.Admin));

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        // GET /api/events/{eventId}/participants/lookup?email=kyle@test.com
        [HttpGet("lookup")]
        public async Task<IActionResult> LookupRegistrant(int eventId, [FromQuery] string? email)
        {
            var result = await _enrollmentService.LookupRegistrantAsync(
                eventId, email, User.GetUserId(), User.IsInRole(Roles.Admin));

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        // POST /api/events/{eventId}/participants/{userId}   (enroll a confirmed existing account)
        [HttpPost("{userId:guid}")]
        public async Task<IActionResult> EnrollExisting(int eventId, Guid userId)
        {
            var result = await _enrollmentService.EnrollExistingAsync(
                eventId, userId, User.GetUserId(), User.IsInRole(Roles.Admin));

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        // POST /api/events/{eventId}/participants/new   (walk-up: creates a placeholder account)
        [HttpPost("new")]
        public async Task<IActionResult> EnrollNewRegistrant(int eventId, NewRegistrantRequestDto request)
        {
            var result = await _enrollmentService.EnrollNewRegistrantAsync(
                eventId, request, User.GetUserId(), User.IsInRole(Roles.Admin));

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return StatusCode(StatusCodes.Status201Created, result.Data);
        }

        // DELETE /api/events/{eventId}/participants/{userId}
        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> Unenroll(int eventId, Guid userId)
        {
            var result = await _enrollmentService.UnenrollAsync(
                eventId, userId, User.GetUserId(), User.IsInRole(Roles.Admin));

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return NoContent();
        }

        // GET /api/events/{eventId}/participants/{userId}/skills
        [HttpGet("{userId:guid}/skills")]
        public async Task<IActionResult> GetParticipantEventSkills(int eventId, Guid userId)
        {
            var result = await _eventSkillProgressService.GetParticipantEventSkillsAsync(
                eventId, userId, User.GetUserId(), User.IsInRole(Roles.Admin));

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        // PUT /api/events/{eventId}/participants/{userId}/skills/progress
        [HttpPut("{userId:guid}/skills/progress")]
        public async Task<IActionResult> SetEventSkillStatuses(
            int eventId, Guid userId, SetEventSkillStatusesRequestDto request)
        {
            var result = await _eventSkillProgressService.SetEventSkillStatusesAsync(
                eventId, userId, request, User.GetUserId(), User.IsInRole(Roles.Admin));

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }
    }
}