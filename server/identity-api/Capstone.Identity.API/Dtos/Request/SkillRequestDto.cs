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
        public string Name { get; init; } = default!;
        [Required]
        [MaxLength(200)]
        public string Description { get; init; } = default!;
        [MaxLength(150)]
        public string? Benefits { get; init; } = null;
        [Required]
        public int? DisciplineId { get; init; } = null;
    }
}
