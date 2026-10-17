using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Models;
using System.Runtime.CompilerServices;

namespace Capstone.Identity.API.Mappings
{
    public static class SkillExtensions
    {
        public static SkillDto ToSkillDto(this Skill skill)
        {
            var skillDto = new SkillDto
            {
                SkillId = skill.SkillId,
                Name = skill.Name,
                Description = skill.Description,
                Benefits = skill.Benefits,
                IsArchived = skill.IsArchived,
                DisciplineId = skill.DisciplineId
            };

            return skillDto;
        }
    }
}
