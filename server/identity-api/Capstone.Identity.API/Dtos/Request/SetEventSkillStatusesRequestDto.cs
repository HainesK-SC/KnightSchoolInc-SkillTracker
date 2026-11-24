using Capstone.Identity.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    public class SetEventSkillStatusesRequestDto
    {
        [Required]
        public List<SkillStatusChangeDto>? Changes { get; set; }
    }

    public class SkillStatusChangeDto
    {
        [Required]
        public int? SkillId { get; set; }

        [Required]
        public SkillStatus? Status { get; set; }
    }
}