using Capstone.Identity.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    public class SetSkillStatusRequestDto
    {
        [Required]
        public SkillStatus? Status { get; set; }
    }
}
