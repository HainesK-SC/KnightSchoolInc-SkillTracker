using Capstone.Identity.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    public class CreateEventDto
    {
        [Required]
        [MaxLength(400)]
        public string EventName { get; set; } = string.Empty;
        [Required]
        public string EventLocation {  get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        [Required]
        public DateTime EventStart { get; set; }
        [Required]
        public DateTime EventEnd { get; set; }
        public EventRegistrationType RegistrationType { get; set; }
    }
}
