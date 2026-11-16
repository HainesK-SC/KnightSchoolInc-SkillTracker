using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventLocation { get; set; } = string.Empty;
        public string? Description { get; set; }
        DateTime EventStart { get; set; } = default;
        DateTime EventEnd { get; set; } = default;
        EventRegistrationType RegistrationType { get; set; }

    }
}
