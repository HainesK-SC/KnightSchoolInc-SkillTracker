namespace Capstone.Identity.API.Auth
{
    public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
}