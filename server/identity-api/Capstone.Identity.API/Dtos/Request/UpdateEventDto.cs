using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Dtos.Request
{
    public class UpdateEventDto
    {
        public string EventName { get; set; } = string.Empty;
        public string EventLocation { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public DateTime EventStart { get; set; } = default;
        public DateTime EventEnd { get; set; } = default;
        public EventRegistrationType RegistrationType { get; set; }
    }
}
