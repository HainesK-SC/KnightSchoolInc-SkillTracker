using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class AdminSkillsController : ControllerBase
    {
        private readonly ISkillService _skillService;

        public AdminSkillsController(ISkillService skillService)
        {
            _skillService = skillService;
        }

        // Disciline Endpoints
        [HttpGet("disciplines")]
        public async Task<IActionResult> GetDisciplines()
        {
            var disciplinesResult = await _skillService.GetDisciplinesAsync();

            if (!disciplinesResult.Succeeded)
            {
                return disciplinesResult.ToProblemResult();
            }

            return Ok(disciplinesResult.Data);
        }

        [HttpGet("disciplines/{id:int}")]
        public async Task<IActionResult> GetDisciplineById(int id)
        {
            var disciplineResult = await _skillService.GetDisciplineAsync(id);

            if (!disciplineResult.Succeeded)
            {
                return disciplineResult.ToProblemResult();
            }

            var discipline = disciplineResult.Data;

            return Ok(discipline);
        }

        [HttpPost("disciplines")]
        public async Task<IActionResult> CreateNewDiscipline(DisciplineRequestDto request)
        {
            var adminId = User.GetUserId();
            var createResult = await _skillService.CreateDisciplineAsync(request, adminId);

            if (!createResult.Succeeded)
            {
                return createResult.ToProblemResult();
            }

            var discipline = createResult.Data;

            return CreatedAtAction(nameof(GetDisciplineById), new { id = discipline.DisciplineId}, discipline);
        }

        [HttpPut("disciplines/{id:int}")]
        public async Task<IActionResult> UpdateDiscipline(int id, DisciplineRequestDto request)
        {
            var adminId = User.GetUserId();

            var updateResult = await _skillService.UpdateDisciplineAsync(id, request, adminId);

            if (!updateResult.Succeeded)
            {
                return updateResult.ToProblemResult();
            }

            return Ok(updateResult.Data);
        }

        [HttpDelete("disciplines/{id:int}")]
        public async Task<IActionResult> DeleteDiscipline(int id)
        {
            var adminId = User.GetUserId();

            var deleteResult = await _skillService.DeleteDisciplineAsync(id, adminId);

            if (!deleteResult.Succeeded)
            {
                return deleteResult.ToProblemResult();
            }

            return NoContent();
        }

        // Skill Endpoints
        [HttpGet("skills")]
        public async Task<IActionResult> GetSkills()
        {
            var skillsResult = await _skillService.GetCatalogAsync();

            if (!skillsResult.Succeeded)
            {
                return skillsResult.ToProblemResult();
            }

            var skills = skillsResult.Data;

            return Ok(skills);
        }

        [HttpGet("skills/{id:int}")]
        public async Task<IActionResult> GetSkill(int id)
        {
            var skillResult = await _skillService.GetSkillAsync(id);

            if (!skillResult.Succeeded)
            {
                return skillResult.ToProblemResult();
            }

            var skillDto = skillResult.Data;

            return Ok(skillDto);
        }

        [HttpPost("skills")]
        public async Task<IActionResult> CreateSkill(SkillRequestDto request)
        {
            var adminId = User.GetUserId();

            var createResult = await _skillService.CreateSkillAsync(request, adminId);

            if (!createResult.Succeeded)
            {
                return createResult.ToProblemResult();
            }

            var skillDto = createResult.Data;

            return CreatedAtAction(nameof(GetSkill), new { id = skillDto.SkillId }, skillDto);
        }

        [HttpPut("skills/{id:int}")]
        public async Task<IActionResult> UpdateSkill(int id, SkillRequestDto request)
        {
            var adminId = User.GetUserId();
            var updateResult = await _skillService.UpdateSkillAsync(id, request, adminId);

            if (!updateResult.Succeeded)
            {
                return updateResult.ToProblemResult();
            }

            var skillDto = updateResult.Data;

            return Ok(skillDto);
        }

        [HttpPost("skills/{id:int}/archive")]
        public async Task<IActionResult> ArchiveSkill(int id)
        {
            var adminId = User.GetUserId();
            var archiveResult = await _skillService.ArchiveSkillAsync(id, adminId);

            if (!archiveResult.Succeeded)
            {
                return archiveResult.ToProblemResult();
            }

            var updatedSkill = archiveResult.Data;

            return Ok(updatedSkill);
        }

        [HttpPost("skills/{id:int}/unarchive")]
        public async Task<IActionResult> UnArchiveSkill(int id)
        {
            var adminId = User.GetUserId();

            var unarchiveResult = await _skillService.UnarchiveSkillAsync(id, adminId);

            if (!unarchiveResult.Succeeded)
            {
                return unarchiveResult.ToProblemResult();
            }

            var updatedSkill = unarchiveResult.Data;

            return Ok(updatedSkill);
        }

        [HttpDelete("skills/{id:int}")]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var adminId = User.GetUserId();

            var deleteResult = await _skillService.DeleteSkillAsync(id, adminId);

            if (!deleteResult.Succeeded)
            {
                return deleteResult.ToProblemResult();
            }

            return NoContent();
        }
    }
}
