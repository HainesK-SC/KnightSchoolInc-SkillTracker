using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Dtos.Request
{
    public class CreateEventDto
    {
        public string EventName { get; set; } = string.Empty;
        public string EventLocation {  get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public EventRegistrationType RegistrationType { get; set; }
        public bool IsCancelled { get; set; } = false;
    }
}
