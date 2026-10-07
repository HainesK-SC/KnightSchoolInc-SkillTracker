using Capstone.Identity.API.Dtos.Auth;

namespace Capstone.Identity.API.Auth
{
    public sealed record AuthResult(CurrentUserResponseDto User, AccessToken Token);
}
