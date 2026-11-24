namespace Capstone.Identity.API.Models
{
    // Participants are keyed on the profile (not the user) because ALL participants have a profile
    // placeholders are participants too. Staff assignments use UserId instead.
    public class EventEnrollment
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int UserProfileId { get; set; }
        public DateTime EnrolledAtUtc { get; set; }
        public Guid EnrolledById { get; set; }
    }
}
