using Capstone.Identity.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    public class UpdateEventDto
    {
        [Required]
        [MaxLength(400)]
        public string EventName { get; set; } = string.Empty;
        [Required]
        public string EventLocation { get; set; } = string.Empty;
        [Required]
        public string? Description { get; set; } = string.Empty;
        [Required]
        public DateTime? EventStart { get; set; } = default;
        [Required]
        public DateTime? EventEnd { get; set; } = default;
        public EventRegistrationType? RegistrationType { get; set; }
    }
}
