using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    /// <summary>
    /// This DTO represents a skill request incoming from the client side.
    /// Both, create and update operations require the same data and will utilize
    /// this DTO.
    /// </summary>
    public class SkillRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = default!;
        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = default!;
        [MaxLength(150)]
        public string? Benefits { get; set; } = null;
        [Required]
        public int? DisciplineId { get; set; } = default!;
    }
}
