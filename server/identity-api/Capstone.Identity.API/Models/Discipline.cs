using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class Discipline
    {
        public int DisciplineId { get; set; } = default;
        [MaxLength(50)]
        public string Name { get; set; } = default!;

    }
}
