using Capstone.Identity.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Admin
{
    public sealed class UpdateUserRequestDto
    {
        [Required, MaxLength(50)]
        public string FirstName { get; init; } = default!;

        [Required, MaxLength(50)]
        public string LastName { get; init; } = default!;

        [Required]
        public DisplayNameSalutations? Salutation { get; init; }

        [Required]
        public DisplayNameModifiers? Modifier { get; init; }

        [EmailAddress, MaxLength(256)]
        public string? Email { get; init; }

        public IReadOnlyList<string> Roles { get; init; } = [];
    }
}
