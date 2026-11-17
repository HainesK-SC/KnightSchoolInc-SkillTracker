using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _dbContext;

        public EventRepository(AppDbContext context)
        {
            _dbContext = context;
        }

        public async Task<Event?> GetEventByIdAsync(int eventId)
        {
            // no EF tracking
            var eventById = await _dbContext.Events
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.EventId == eventId);

            return eventById;
        }
        public async Task<Event?> GetEventByIdForUpdateAsync(int eventId)
        {
            // tracked by EF
            var eventForUpdate = await _dbContext.Events
                .SingleOrDefaultAsync(e => e.EventId == eventId);

            return eventForUpdate;
        }
        public async Task<Event> AddEventAsync(Event newEvent)
        {
            await _dbContext.AddAsync<Event>(newEvent);

            return newEvent;
        }
        public void RemoveEvent(Event removeEvent)
        {
            _dbContext.Remove(removeEvent);
        }

        public async Task<Event?> GetEventByNameAsync(string eventName)
        {
           var eventByName = await _dbContext.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventName == eventName);

            return eventByName;
                
        }

        public async Task<IReadOnlyList<Event>> SearchEventsAsync(
            string? search,
            DateTime? fromUtc,
            DateTime? toUtc,
            bool includeCancelled,
            Guid? staffUserId,
            int maxResults)
        {
            var query = _dbContext.Events.AsNoTracking();

            if (!includeCancelled)
            {
                query = query.Where(e => !e.IsCancelled);
            }

            // Instructors: only events they're assigned to
            if (staffUserId is not null)
            {
                var userId = staffUserId.Value;

                query = query.Where(e => _dbContext.EventsStaffAssignments
                    .Any(a => a.EventId == e.EventId && a.UserId == userId));
            }

            // Date overlap: the event must end on/after "from" and start on/before "to"
            if (fromUtc is not null)
            {
                var from = fromUtc.Value;
                query = query.Where(e => e.EventEnd >= from);
            }

            if (toUtc is not null)
            {
                var to = toUtc.Value;
                query = query.Where(e => e.EventStart <= to);
            }

            // One search box: event name, location, or an assigned instructor's name
            if (search is not null)
            {
                var term = search;

                var eventIdsWithMatchingStaff =
                    from assignment in _dbContext.EventsStaffAssignments
                    join user in _dbContext.ApplicationUsers
                        on assignment.UserId equals user.Id
                    select assignment.EventId;

                query = query.Where(e =>
                    e.EventName.Contains(term)
                    || e.EventLocation.Contains(term)
                    || eventIdsWithMatchingStaff.Contains(e.EventId));
            }

            return await query
                .OrderBy(e => e.EventStart)
                .ThenBy(e => e.EventName)
                .Take(maxResults)
                .ToListAsync();
        }
    }
}
