using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Models.ReadModels;

namespace Capstone.Identity.API.Mappings
{
    public static class EventStaffAssignmentExtensions
    {
        public static EventStaffDto ToEventStaffDto(this EventStaffMember staffMember)
        {
            var eventStaffDto = new EventStaffDto
            {
                Id = staffMember.Id,
                EventId = staffMember.EventId,
                UserId = staffMember.UserId,
                FirstName = staffMember.FirstName,
                LastName = staffMember.LastName,
                AssignedAtUtc = staffMember.AssignedAtUtc,
                AssignedBy = staffMember.AssignedBy
            };

            return eventStaffDto;
        }
    }
}
