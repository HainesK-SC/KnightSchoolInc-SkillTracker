using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    public class NewRegistrantRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        // Optional. Send null (not "") when there's no email
        [EmailAddress]
        [MaxLength(256)]
        public string? Email { get; set; }
    }
}