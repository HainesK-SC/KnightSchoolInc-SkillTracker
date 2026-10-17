using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos
{
    public class DisciplineDto
    {
        public int DisciplineId { get; set; } = default!;
        [MaxLength(50)]
        public string Name { get; set; } = default!;
    }
}
