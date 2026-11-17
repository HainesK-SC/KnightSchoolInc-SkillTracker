using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Capstone.Identity.API.Models
{
    [Index(nameof(EventId), nameof(UserProfileId), IsUnique = true)]
    public class EventStaffAssignment
    {
        public int EventId { get; set; } = default;
        public int UserProfileId { get; set; } = default;
        public DateTime AssignedAtUtc = default;
        public int AssignedBy = default;
    }
}
