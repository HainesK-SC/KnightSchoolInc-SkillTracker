using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Response;

namespace Capstone.Identity.API.Services
{
    public interface IEventStaffService
    {
        Task<Result<List<EventStaffDto>>> AssignStaffAsync(int eventId, Guid userId, Guid actingAdminId);
        Task<Result<List<EventStaffDto>>> UnassignStaffAsync(int eventId, Guid userId, Guid actingAdminId);
        Task<Result<List<EventStaffDto>>> GetEventStaffAsync(int eventId);
    }
}
