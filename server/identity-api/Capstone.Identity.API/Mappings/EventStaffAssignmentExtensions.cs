using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Mappings
{
    public static class EventStaffAssignmentExtensions
    {
        public static EventStaffDto ToEventStaffDto(this EventStaffAssignment eventStaffAssignment)
        {
            var eventStaffDto = new EventStaffDto
            {
                Id = eventStaffAssignment.Id,
                EventId = eventStaffAssignment.Id,
                UserProfileId = eventStaffAssignment.UserProfileId,
                AssignedAtUtc = eventStaffAssignment.AssignedAtUtc,
                AssignedBy = eventStaffAssignment.AssignedBy
            };

            return eventStaffDto;
        }
    }
}
