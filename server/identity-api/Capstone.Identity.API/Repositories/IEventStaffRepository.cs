using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface IEventStaffRepository
    {
        public Task<EventStaffAssignment> AddStaffAssignmentAsync(EventStaffAssignment staffAssignment);
        public void RemoveStaffAssignment(EventStaffAssignment staffAssignment);
        public Task<List<EventStaffAssignment>> GetEventStaffForEventAsync(int eventId);
        public Task<EventStaffAssignment> GetEventStaffAssignmentForUpdateAsync(int eventId, Guid userId);
        public Task<bool> IsAssignedAsync(int eventId, Guid userId);
    }
}
