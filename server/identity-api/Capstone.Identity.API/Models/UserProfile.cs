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
        public int ApplicationUserId { get; set; }

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser ApplicationUser { get; set; }
    }
}
