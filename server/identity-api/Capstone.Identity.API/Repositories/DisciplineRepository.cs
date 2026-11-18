using Capstone.Identity.API.Data;
using Capstone.Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Repositories
{
    public class DisciplineRepository : IDisciplineRepository
    {
        private readonly AppDbContext _appDbContext;

        public DisciplineRepository( AppDbContext context)
        {
            _appDbContext = context;
        }

        public void AddNewDiscipline(Discipline discipline)
        { 
            _appDbContext.Disciplines.Add(discipline);

        }
        public async Task<bool> DisciplineExistsByName(string name, int? excludingId = null)
        {
            var query = _appDbContext.Disciplines
                .AsNoTracking()
                .Where(d => d.Name == name);

            if (excludingId is not null)
            {
                query = query.Where(d => d.DisciplineId != excludingId.Value);
            }

            return await query.AnyAsync();
        }
        public async Task<IReadOnlyList<Discipline>> GetAllDisciplinesAsync() 
        {
            var disciplines = await _appDbContext.Disciplines
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync<Discipline>();

            return disciplines;
        }

        public async Task<Discipline?> GetDisciplineByIdAsync(int id) 
        {
            var discipline = await _appDbContext.Disciplines
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.DisciplineId == id);

            return discipline;
        }

        public async Task<Discipline?> GetDisciplineByIdForUpdate(int id) 
        {
            var discipline = await _appDbContext.Disciplines.FirstOrDefaultAsync(d => d.DisciplineId == id);

            return discipline;
        }

        public void RemoveDiscipline(Discipline discipline) 
        {
            _appDbContext.Disciplines.Remove(discipline);
        }

        public async Task<bool> AnyInDisciplineAsync(int disciplineId)
        {
            return await _appDbContext.Skills.AnyAsync(s => s.DisciplineId == disciplineId);
        }
    }
}