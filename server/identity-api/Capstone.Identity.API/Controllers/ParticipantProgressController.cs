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
        private readonly ISkillTreeService _skillTreeService;

        public ParticipantProgressController(ISkillProgressService skillProgressService, ISkillTreeService skillTreeService)
        {
            _skillProgressService = skillProgressService;
            _skillTreeService = skillTreeService;
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

        [HttpGet("{userId:guid}/skill-tree")]
        public async Task<IActionResult> GetSkillTree(Guid userId)
        {
            var skillTree = await _skillTreeService.GetSkillTreeAsync(userId);

            if (!skillTree.Succeeded)
            {
                return skillTree.ToProblemResult();
            }
            
            return Ok(skillTree.Data);
        }
    }
}
