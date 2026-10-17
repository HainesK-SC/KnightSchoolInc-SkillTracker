using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Response
{
    public class SkillDto
    {
        public int SkillId { get; set; } = default;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string? Benefits { get; set; } = default;
        public bool IsArchived { get; set; } = false;
        // FK to Discipline parent
        public int DisciplineId { get; set; } = default!;
    }
}
