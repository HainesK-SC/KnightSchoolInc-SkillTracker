using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Capstone.Identity.API.Auth
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required] public string Issuer { get; init; } = default!;
        [Required] public string Audience { get; init; } = default!;
        [Required, MinLength(32)] public string Key { get; init; } = default!;
        [Range(1, 1440)] public int ExpiryMinutes { get; init; }

        public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(Key));
    }
}
