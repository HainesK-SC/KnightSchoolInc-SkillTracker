using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Response
{
    public class SkillDto
    {
        public int SkillId { get; init; }
        public string Name { get; init; } = default!;
        public string Description { get; init; } = default!;
        public string? Benefits { get; init; } = default;
        public bool IsArchived { get; init; } = false;
        // FK to Discipline parent
        public int DisciplineId { get; init; }
    }
}
