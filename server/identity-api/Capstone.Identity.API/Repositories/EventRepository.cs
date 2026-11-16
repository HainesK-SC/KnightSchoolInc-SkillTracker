using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public class EventRepository
    {
        private readonly AppDbContext _dbContext;

        public EventRepository(AppDbContext context)
        {
            _dbContext = context;
        }

        public async Task<Event> GetEventByIdAsync(int eventId)
        {
            // no EF tracking
            var eventById = await _dbContext.Events
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.EventId == eventId);

            return eventById;
        }
        public async Task<Event> GetEventByIdForUpdateAsync(int eventId)
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
    }
}
