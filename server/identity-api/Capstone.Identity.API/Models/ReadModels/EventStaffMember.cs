namespace Capstone.Identity.API.Models.ReadModels
{
    public class EventStaffMember
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime AssignedAtUtc { get; set; }
        public Guid AssignedBy { get; set; }
    }
}
