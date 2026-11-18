namespace Capstone.Identity.API.Dtos.Request
{
    public class AssignEventStaffDto
    {
        public int EventId { get; set; } = default;
        public Guid UserId { get; set; } = default;
        public DateTime AssignedAtUtc = default;
        public Guid AssignedBy = default;
    }
}
