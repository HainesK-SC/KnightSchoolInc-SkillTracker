using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Response;

namespace Capstone.Identity.API.Services
{
    public interface IEventStaffService
    {
        Task<Result<EventStaffDto>> AssignStaffAsync(int eventId, int userProfileId, Guid actingAdminId);
        Task<Result<EventStaffDto>> UnassignStaffAsync(int eventId, int userProfileId, Guid actingAdminId);
        Task<Result<IReadOnlyList<EventStaffDto>>> GetEventStaffAsync(int eventId);
    }
}
