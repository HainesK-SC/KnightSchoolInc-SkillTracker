using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Models.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public class EventEnrollmentRepository : IEventEnrollmentRepository
    {
        private readonly AppDbContext _context;

        public EventEnrollmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public void AddEnrollment(EventEnrollment enrollment)
        {
            _context.EventEnrollments.Add(enrollment);
        }

        public void RemoveEnrollment(EventEnrollment enrollment)
        {
            _context.EventEnrollments.Remove(enrollment);
        }

        public Task<bool> IsEnrolledAsync(int eventId, int userProfileId)
        {
            return _context.EventEnrollments
                .AnyAsync(e => e.EventId == eventId && e.UserProfileId == userProfileId);
        }

        public Task<EventEnrollment?> GetEnrollmentForUpdateAsync(int eventId, int userProfileId)
        {
            // Tracked: the service removes it
            return _context.EventEnrollments
                .SingleOrDefaultAsync(e => e.EventId == eventId && e.UserProfileId == userProfileId);
        }

        public Task<bool> AnyForEventAsync(int eventId)
        {
            return _context.EventEnrollments.AnyAsync(e => e.EventId == eventId);
        }

        public Task<bool> HasOverlappingEnrollmentAsync(
            int userProfileId,
            DateTime startUtc,
            DateTime endUtc,
            int excludingEventId)
        {
            // Is this person enrolled in any OTHER non-cancelled event whose dates overlap?
            var overlapping =
                from enrollment in _context.EventEnrollments
                join ev in _context.Events
                    on enrollment.EventId equals ev.EventId
                where enrollment.UserProfileId == userProfileId
                    && enrollment.EventId != excludingEventId
                    && !ev.IsCancelled
                    && ev.EventStart < endUtc
                    && ev.EventEnd > startUtc
                select enrollment.Id;

            return overlapping.AnyAsync();
        }

        public async Task<IReadOnlyList<RosterEntry>> GetRosterAsync(int eventId, string? search, int maxResults)
        {
            var roster =
                from enrollment in _context.EventEnrollments
                join profile in _context.UserProfiles
                    on enrollment.UserProfileId equals profile.UserProfileId
                join user in _context.Users
                    on profile.ApplicationUserId equals user.Id
                where enrollment.EventId == eventId
                select new { enrollment, profile, user };

            if (search is not null)
            {
                var term = search;

                roster = roster.Where(r =>
                    r.user.FirstName.Contains(term)
                    || r.user.LastName.Contains(term)
                    || (r.user.FirstName + " " + r.user.LastName).Contains(term)
                    || r.profile.DisplayName.Contains(term)
                    || (r.user.Email != null && r.user.Email.Contains(term)));
            }

            return await roster
                .OrderBy(r => r.user.LastName)
                .ThenBy(r => r.user.FirstName)
                .Take(maxResults)
                .Select(r => new RosterEntry
                {
                    UserId = r.user.Id,
                    FirstName = r.user.FirstName,
                    LastName = r.user.LastName,
                    DisplayName = r.profile.DisplayName,
                    Email = r.user.Email,
                    HasPassword = r.user.PasswordHash != null,
                    EnrolledAtUtc = r.enrollment.EnrolledAtUtc
                })
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
