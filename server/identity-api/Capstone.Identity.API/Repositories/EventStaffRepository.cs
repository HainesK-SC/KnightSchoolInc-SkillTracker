using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public class EventStaffRepository : IEventStaffRepository
    {
        private readonly AppDbContext _context;

        public EventStaffRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EventStaffAssignment> AddStaffAssignmentAsync(EventStaffAssignment staffAssignment)
        {
            await _context.AddAsync<EventStaffAssignment>(staffAssignment);

            return staffAssignment;
        }

        public async Task<EventStaffAssignment> GetEventStaffAssignmentForUpdateAsync(int eventId, Guid userId)
        {
            var eventStaffForUpdate = await _context.EventsStaffAssignments
                .Where(a => a.EventId == eventId && a.UserId == userId)
                .SingleOrDefaultAsync();

            return eventStaffForUpdate;
        }

        public async Task<List<EventStaffAssignment>> GetEventStaffForEventAsync(int eventId)
        {
            var eventStaff = await _context.EventsStaffAssignments
                .Where(a => a.EventId == eventId)
                .AsNoTracking()
                .ToListAsync();

            return eventStaff;
        }

        public async Task<bool> IsAssignedAsync(int eventId, Guid userId)
        {
            var isAssigned = false;

            foreach (var e in _context.EventsStaffAssignments)
            {
                if (e.EventId == eventId && e.UserId == userId)
                {
                    isAssigned = true;
                }
            }

            return isAssigned;
        }

        public void RemoveStaffAssignment(EventStaffAssignment staffAssignment)
        {
            _context.Remove(staffAssignment);
        }
    }
}