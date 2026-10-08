using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Admin;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class AdminUsersController : ControllerBase
    {
        private readonly IApplicationUserService _userService;

        public AdminUsersController(IApplicationUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("roles")]
        public IActionResult GetRoles()
        {
            return Ok(Roles.GetAllRoles());
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var result = await _userService.GetUsersForAdminAsync();
            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        [HttpGet("users/{id:guid}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var result = await _userService.GetUserForAdminAsync(id);
            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser(CreateUserRequestDto request)
        {
            var result = await _userService.CreateUserAsAdminAsync(request);
            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            var createdUser = result.Data!;
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut("users/{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequestDto request)
        {
            var result = await _userService.UpdateUserAsAdminAsync(id, request, User.GetUserId());
            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return Ok(result.Data);
        }

        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _userService.DeleteUserAsAdminAsync(id, User.GetUserId());
            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            return NoContent();
        }
    }
}
