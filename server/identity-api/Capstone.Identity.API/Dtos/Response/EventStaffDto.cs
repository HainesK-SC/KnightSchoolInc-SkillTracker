namespace Capstone.Identity.API.Dtos.Response
{
    public class EventStaffDto
    {
        public int Id { get; set; }
        public int EventId { get; set; } = default;
        public Guid UserId { get; set; } = default;
        public DateTime AssignedAtUtc = default;
        public Guid AssignedBy = default;
    }
}
