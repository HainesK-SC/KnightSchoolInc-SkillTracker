using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface IEventRepository
    {
        public Task<Event> GetEventByIdAsync(int eventId); // as untracked by EF
        public Task<Event> GetEventByIdForUpdateAsync(int eventId); // tracked by EF
        public Task<Event> AddEventAsync(Event newEvent);
        public void RemoveEvent(Event removeEvent);
        public Task<Event> GetEventByNameAsync(string name);
    }
}
