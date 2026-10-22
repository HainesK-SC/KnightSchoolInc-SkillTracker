using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Controllers
{
    [Route("api/skills")]
    [ApiController]
    [Authorize]
    public class SkillTreeController : ControllerBase
    {
        private readonly ISkillTreeService _skillTreeService;

        public SkillTreeController(ISkillTreeService skillTreeService)
        {
            _skillTreeService = skillTreeService;
        }

        [HttpGet("tree")]
        public async Task<IActionResult> GetMySkillTree()
        {
            var userId = User.GetUserId();

            var result = await _skillTreeService.GetSkillTreeAsync(userId);
            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }
    }
}
