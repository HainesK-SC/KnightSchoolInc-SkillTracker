using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Controllers
{
    [ApiController]
    [Route("api/admin/placeholders")]
    [Authorize(Roles = Roles.Admin)]
    public class AdminPlaceholdersController : ControllerBase
    {
        private readonly IPlaceholderService _placeholderService;

        public AdminPlaceholdersController(IPlaceholderService placeholderService)
        {
            _placeholderService = placeholderService;
        }

        // GET /api/admin/placeholders/{placeholderUserId}/merge/{targetUserId}
        [HttpGet("{placeholderUserId:guid}/merge/{targetUserId:guid}")]
        public async Task<IActionResult> PreviewMerge(Guid placeholderUserId, Guid targetUserId)
        {
            var result = await _placeholderService.PreviewMergeAsync(placeholderUserId, targetUserId);

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        // POST /api/admin/placeholders/{placeholderUserId}/merge/{targetUserId}
        [HttpPost("{placeholderUserId:guid}/merge/{targetUserId:guid}")]
        public async Task<IActionResult> Merge(Guid placeholderUserId, Guid targetUserId)
        {
            var result = await _placeholderService.MergeAsync(placeholderUserId, targetUserId, User.GetUserId());

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return NoContent();
        }

        // DELETE /api/admin/placeholders/{placeholderUserId}
        [HttpDelete("{placeholderUserId:guid}")]
        public async Task<IActionResult> DeletePlaceholder(Guid placeholderUserId)
        {
            var result = await _placeholderService.DeletePlaceholderAsync(placeholderUserId, User.GetUserId());

            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return NoContent();
        }
    }
}