using Capstone.Identity.API.Dtos.Response;

namespace Capstone.Identity.API.Models
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
    }
}
