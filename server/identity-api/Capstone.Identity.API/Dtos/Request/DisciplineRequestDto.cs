using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    /// <summary>
    /// Used for requests that carry Discipline information (create and update requests).
    /// These two operations require the same data for the operation.
    /// </summary>
    public class DisciplineRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; init; } = default!;
    }
}
