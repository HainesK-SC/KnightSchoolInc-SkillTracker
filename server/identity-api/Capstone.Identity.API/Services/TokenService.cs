using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Capstone.Identity.API.Services
{
    public class TokenService : ITokenService
    {
        //private readonly IConfiguration _configuration;
        private readonly JwtOptions _jwt;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(IOptions<JwtOptions> jwt, UserManager<ApplicationUser> userManager)
        {
            _jwt = jwt.Value;
            _userManager = userManager;
        }

        public async Task<AccessToken> GenerateTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var appUserId = Convert.ToString(user.Id);
            // var appUserEmail = user.Email; - causes conflict for null profile

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                // new Claim(ClaimTypes.Email, appUserEmail) - removed for null profile conflict
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(_jwt.ExpiryMinutes);

            // var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            //var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //var expiryMinutes = double.Parse(_configuration["Jwt:ExpiryMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: new SigningCredentials(_jwt.GetSigningKey(), SecurityAlgorithms.HmacSha256));

            return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}