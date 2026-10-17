using Capstone.Identity.API.Models;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    /// <summary>
    /// Represents a discipline catalog used to transfer discipline metadata between application layers.
    /// </summary>
    /// <remarks>Intended for serialization and mapping between domain and presentation layers. Avoid
    /// embedding business logic; keep properties simple and serializable.</remarks>
    public class DisciplineCatalogDto
    {
        public int DisciplineId { get; init; }
        [MaxLength(50)]
        public string Name { get; init; } = default!;
        public List<Skill> Skills { get; init; } = new();
    }
}
