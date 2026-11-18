using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    public class SetEventSkillsRequestDto
    {
        [Required]
        public List<int>? SkillIds { get; set; }
    }
}
