using Capstone.Identity.API.Models;
using Capstone.Identity.API.Models.ReadModels;

namespace Capstone.Identity.API.Repositories
{
    public interface IEventEnrollmentRepository
    {
        void AddEnrollment(EventEnrollment enrollment);
        void RemoveEnrollment(EventEnrollment enrollment);
        Task<bool> IsEnrolledAsync(int eventId, int userProfileId);
        Task<EventEnrollment?> GetEnrollmentForUpdateAsync(int eventId, int userProfileId);
        Task<bool> AnyForEventAsync(int eventId);
        Task<bool> HasOverlappingEnrollmentAsync(int userProfileId, DateTime startUtc, DateTime endUtc, int excludingEventId);
        Task<IReadOnlyList<RosterEntry>> GetRosterAsync(int eventId, string? search, int maxResults);
        Task<List<EventEnrollment>> GetForProfileForUpdateAsync(int userProfileId);
    }
}
