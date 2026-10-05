namespace Capstone.Identity.API.Dtos.Auth
{
    public sealed class CurrentUserResponseDto
    {
        public Guid Id { get; init; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarImagePath { get; set; } = string.Empty;
        public IReadOnlyList<string> Roles { get; set; } = [];

    }
}
