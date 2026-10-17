using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Response
{
    public class DisciplineDto
    {
        public int DisciplineId { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
