using Capstone.Identity.API.Data;
using Capstone.Identity.API.Dtos.Request;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Models;
using System.Runtime.CompilerServices;

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

        public static DisciplineCatalogDto ToDisciplineCatalogDto(this Discipline discipline, List<SkillDto> skills)
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
