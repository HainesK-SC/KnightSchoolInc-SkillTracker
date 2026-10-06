using System.Security.Claims;

namespace Capstone.Identity.API.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new InvalidOperationException(
                    "The authenticated user has no valid user ID claim.");
        }
    }
}
