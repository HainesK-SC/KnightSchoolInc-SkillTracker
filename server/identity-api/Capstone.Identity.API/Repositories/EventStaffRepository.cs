using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Models.ReadModels;
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

        public async Task<List<EventStaffMember>> GetEventStaffForEventAsync(int eventId)
        {
            var staff =
           from assignment in _context.EventsStaffAssignments
           join user in _context.Users
               on assignment.UserId equals user.Id
           where assignment.EventId == eventId
           orderby user.LastName, user.FirstName
           select new EventStaffMember
       {
           Id = assignment.Id,
           EventId = assignment.EventId,
           UserId = assignment.UserId,
           FirstName = user.FirstName,
           LastName = user.LastName,
           AssignedAtUtc = assignment.AssignedAtUtc,
           AssignedBy = assignment.AssignedBy
       };

            return await staff.AsNoTracking().ToListAsync();
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

        public Task<bool> AnyForEventAsync(int eventId)
        {
            return _context.EventsStaffAssignments.AnyAsync(a => a.EventId == eventId);
        }
    }
}