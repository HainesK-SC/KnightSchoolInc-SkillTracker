using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
namespace Capstone.Identity.API.Auth
{
    public static class AuthCookie
    {
        public const string Name = "knightschool_auth";

        public static CookieOptions Options(DateTimeOffset expires) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expires
        };
    }
}
