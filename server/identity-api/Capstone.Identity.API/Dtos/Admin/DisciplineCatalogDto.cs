using Capstone.Identity.API.Dtos.Response;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Admin
{
    /// <summary>
    /// Represents a discipline catalog used to transfer discipline metadata between application layers.
    /// </summary>
    /// <remarks>Intended for serialization and mapping between domain and presentation layers. Avoid
    /// embedding business logic; keep properties simple and serializable.</remarks>
    public class DisciplineCatalogDto
    {
        public int DisciplineId { get; set; } = default;
        [MaxLength(50)]
        public string Name { get; set; } = default!;
        public List<SkillDto> Skills { get; set; } = new();
    }
}
