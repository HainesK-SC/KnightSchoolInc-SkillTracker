using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Dtos.Request
{
    public class CreateEventDto
    {
        public string EventName { get; set; } = string.Empty;
        public string EventLocation {  get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        DateTime EventStart { get; set; }
        DateTime EventEnd { get; set; }
        EventRegistrationType RegistrationType { get; set; }
    }
}
