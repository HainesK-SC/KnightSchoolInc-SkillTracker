using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventLocation { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventStart { get; set; } = default;
        public DateTime EventEnd { get; set; } = default;
        public EventRegistrationType RegistrationType { get; set; }
        public bool IsCancelled { get; set; } = false;

    }
}
