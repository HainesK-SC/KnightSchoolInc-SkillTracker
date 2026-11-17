using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Capstone.Identity.API.Models
{
    [Index(nameof(EventId), nameof(UserId), IsUnique = true)]
    public class EventStaffAssignment
    {
        public int Id { get; set; }
        public int EventId { get; set; } = default;
        public Guid UserId { get; set; } = default;
        public DateTime AssignedAtUtc = default;
        public Guid AssignedBy = default;
    }
}
