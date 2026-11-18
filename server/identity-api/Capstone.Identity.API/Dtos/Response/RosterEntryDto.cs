namespace Capstone.Identity.API.Dtos.Response
{
    // represents ONE given participant registered for the specific event
    public class RosterEntryDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string AccountStatus { get; set; } = string.Empty;   // "WalkUp", "PendingActivation", "Active"
        public DateTime EnrolledAtUtc { get; set; }
    }
}