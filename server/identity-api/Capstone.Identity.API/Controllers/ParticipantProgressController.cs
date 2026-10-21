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
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = Roles.Admin + "," + Roles.Instructor)]
    public class ParticipantProgressController : ControllerBase
    {
        private readonly ISkillProgressService _skillProgressService;

        public ParticipantProgressController(ISkillProgressService skillProgressService)
        {
            _skillProgressService = skillProgressService;
        }

        [HttpPut("{userId:guid}/skills/{skillId:int}/progress")]
        public async Task<IActionResult> SetSkillStatus(
            Guid userId,
            int skillId,
            SetSkillStatusRequestDto request)
        {
            var actingUserId = User.GetUserId();

            var result = await _skillProgressService.SetSkillStatusAsync(
                userId,
                skillId,
                request.Status!.Value,
                actingUserId);

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result);
        }
    }
}
