using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;

namespace Capstone.Identity.API.Services
{
    public interface IEventEnrollmentService
    {
        Task<Result<RegistrantLookupDto>> LookupRegistrantAsync(int eventId, string? email, Guid callerUserId, bool callerIsAdmin);
        Task<Result<RosterEntryDto>> EnrollExistingAsync(int eventId, Guid userId, Guid callerUserId, bool callerIsAdmin);
        Task<Result<RosterEntryDto>> EnrollNewRegistrantAsync(int eventId, NewRegistrantRequestDto request, Guid callerUserId, bool callerIsAdmin);
        Task<Result<bool>> UnenrollAsync(int eventId, Guid userId, Guid callerUserId, bool callerIsAdmin);
        Task<Result<List<RosterEntryDto>>> GetRosterAsync(int eventId, string? search, Guid callerUserId, bool callerIsAdmin);
    }
}