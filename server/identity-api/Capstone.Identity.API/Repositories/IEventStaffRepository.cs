using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface IEventStaffRepository
    {
        public Task<EventStaffAssignment> AddStaffAssignmentAsync(EventStaffAssignment staffAssignment);
        public void RemoveStaffAssignment(EventStaffAssignment staffAssignment);
        public Task<IReadOnlyList<EventStaffAssignment>> GetEventStaffForEventAsync(int eventId);
        public Task<List<EventStaffAssignment>> GetEventStaffAssignmentForUpdateAsync(int eventId);
        public Task<bool> IsAssignedAsync(int eventId, int profileId);
    }
}
