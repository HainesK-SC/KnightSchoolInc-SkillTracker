using Capstone.Identity.API.Models;

namespace Capstone.Identity.API.Repositories
{
    public interface IDisciplineRepository
    {
        public Task<ICollection<Discipline>> GetAllDisciplinesAsync();
        public Task<Discipline> GetDisciplineByIdAsync(int id);
        public Task<bool> DisciplineExistsByName(string name, int? excludingId = null);
        public Task AddNewDiscipline(Discipline discipline);
        public Task<Discipline> GetDisciplineByIdForUpdate(int id);
        public void RemoveDiscipline(Discipline discipline);
    }
}
