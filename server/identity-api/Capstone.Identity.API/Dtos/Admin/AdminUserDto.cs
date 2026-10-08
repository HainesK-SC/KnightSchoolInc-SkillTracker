using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Dtos.Admin
{
    public sealed class AdminUserDto
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = default!;
        public string LastName { get; init; } = default!;
        public string? Email { get; init; }
        public DisplayNameSalutations Salutation { get; init; }
        public DisplayNameModifiers Modifier { get; init; }
        public string DisplayName { get; init; } = default!;
        public string? AvatarImagePath { get; init; }
        public AccountStatus Status { get; init; }
        public IReadOnlyList<string> Roles { get; init; } = [];
    }
}
