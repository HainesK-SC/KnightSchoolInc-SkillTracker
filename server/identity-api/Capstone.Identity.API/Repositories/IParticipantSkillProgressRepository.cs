using Capstone.Identity.API.Models;
using System.Net.NetworkInformation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Capstone.Identity.API.Repositories
{
    public interface IParticipantSkillProgressRepository
    {
        public Task<IReadOnlyList<ParticipantSkillProgress>> GetForUserAsync(int userProfileId); //all of one user’s rows, for the tree
        public Task<ParticipantSkillProgress?> GetForUpdateAsync(int userProfileId, int skillId); //tracked, for setting a status
        public void AddNewProgressRow(ParticipantSkillProgress progress);
        public void RemoveProgressRow(ParticipantSkillProgress progress);
        public Task<bool> AnyProgressForSkillAsync(int skillId);
        public Task<bool> AnyProgressInDisciplineAsync(int disciplineId);
    }
}
