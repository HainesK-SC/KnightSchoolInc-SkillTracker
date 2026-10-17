using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Mappings
{
    public static class DisciplineExtensions
    {
        public static DisciplineDto ToDisciplineDto(this Discipline discipline)
        {
            var discipelineDto = new DisciplineDto
            {
                DisciplineId = discipline.DisciplineId,
                Name = discipline.Name
            };

            return discipelineDto;
        }

        public static DisciplineCatalogDto ToDisciplineCatalogDto(this Discipline discipline, List<Skill> skills)
        {
            var disciplineCatalogDto = new DisciplineCatalogDto
            {
                DisciplineId = discipline.DisciplineId,
                Name = discipline.Name,
                Skills = skills
            };

            return disciplineCatalogDto;
        }
    }
}
