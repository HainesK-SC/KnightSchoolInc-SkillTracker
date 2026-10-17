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
        public string Name { get; set; } = default!;
    }
}
