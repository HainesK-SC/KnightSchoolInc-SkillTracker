namespace Capstone.Identity.API.Dtos.Request
{
    public class AssignEventStaffDto
    {
        public int EventId { get; set; } = default;
        public int UserProfileId { get; set; } = default;
        public DateTime AssignedAtUtc = default;
        public int AssignedBy = default;
    }
}
