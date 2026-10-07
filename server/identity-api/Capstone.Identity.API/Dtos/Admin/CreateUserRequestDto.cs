using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Admin
{
    public sealed class CreateUserRequestDto
    {
        [Required, MaxLength(50)]
        public string FirstName { get; init; } = default!;

        [Required, MaxLength(50)]
        public string LastName { get; init; } = default!;

        [EmailAddress, MaxLength(256)]
        public string? Email { get; init; }

        public IReadOnlyList<string> Roles { get; init; } = [];
    }
}
