using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Response
{
    public class DisciplineDto
    {
        public int DisciplineId { get; init; } = default!;
        public string Name { get; init; } = default!;
    }
}
