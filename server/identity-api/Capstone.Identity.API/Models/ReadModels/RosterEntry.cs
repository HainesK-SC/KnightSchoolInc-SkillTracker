namespace Capstone.Identity.API.Models.ReadModels
{
    public class RosterEntry
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool HasPassword { get; set; }
        public DateTime EnrolledAtUtc { get; set; }
    }
}
