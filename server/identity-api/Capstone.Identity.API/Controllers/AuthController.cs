using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Dtos.Auth;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IApplicationUserService _userService;

        public AuthController(IApplicationUserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto registerRequest)
        {
            var result = await _userService.RegisterAsync(registerRequest);
            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            Response.SetAuthCookie(result.Data!.Token);
            return Ok(result.Data.User);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var result = await _userService.LoginAsync(request);
            if (!result.Succeeded)
            {
                return result.ToProblemResult();
            }

            Response.SetAuthCookie(result.Data!.Token);
            return Ok(result.Data.User);
        }
    }
}
