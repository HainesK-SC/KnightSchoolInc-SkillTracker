namespace Capstone.Identity.API.Dtos.Response
{
    public class SkillTreeDto
    {
        public Guid UserId { get; set; }
        public List<SkillTreeDisciplineDto> Disciplines { get; set; }
    }
}
