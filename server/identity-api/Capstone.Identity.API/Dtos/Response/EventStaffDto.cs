namespace Capstone.Identity.API.Dtos.Response
{
    public class EventStaffDto
    {
        public int Id { get; set; }
        public int EventId { get; set; } = default;
        public Guid UserId { get; set; } = default;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime AssignedAtUtc = default;
        public Guid AssignedBy = default;
    }
}
