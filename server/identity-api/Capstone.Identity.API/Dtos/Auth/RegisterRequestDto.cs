using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Auth
{
    public sealed class RegisterRequestDto
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;

        [Required, MaxLength(50)]
        public string FirstName { get; init; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; init; } = string.Empty;
    }
}
