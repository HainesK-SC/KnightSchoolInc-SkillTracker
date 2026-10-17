using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos
{
    public class SkillDto
    {
        public int SkillId { get; set; } = default;
        [MaxLength(50)]
        public string Name { get; set; } = default!;
        [MaxLength(200)]
        public string Description { get; set; } = default!;
        [MaxLength(150)]
        public string? Benefits { get; set; } = default;
        public bool IsArchived { get; set; } = false;
        // FK to Discipline parent
        public int DisciplineId { get; set; } = default!;
    }
}
