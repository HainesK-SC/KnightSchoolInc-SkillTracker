namespace Capstone.Identity.API.Dtos.Response
{
    public class SkillTreeDisciplineDto
    {
        public int DisciplineId { get; set; }
        public string Name { get; set; }
        public List<SkillTreeSkillDto> Skills = new();
    }
}
