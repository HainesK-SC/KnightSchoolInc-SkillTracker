using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Identity.API.Models
{
    public class UserProfile
    {
        public int UserProfileId { get; set; }
        public string DisplayName { get; set; }
        public string AvatarImagePath { get; set; }

        // Props to add later 
        // ICollection<ParticipantSkillProgress> - navigational

        // Navigational
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }
}
