using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Identity.API.Models
{
    public class UserProfile
    {
        public int UserProfileId { get; set; }
        [MaxLength(100)]
        public string DisplayName { get; set; }
        public string? AvatarImagePath { get; set; }

        // Props to add later 
        // ICollection<ParticipantSkillProgress> - navigational

        // FK Reference
        public Guid ApplicationUserId { get; set; }
    }
}
