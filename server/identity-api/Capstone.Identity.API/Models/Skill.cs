using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Identity.API.Models
{
    [Index(nameof(Name), nameof(DisciplineId), IsUnique = true)]
    public class Skill
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
