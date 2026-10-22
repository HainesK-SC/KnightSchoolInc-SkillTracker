using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Response;
using Capstone.Identity.API.Mappings;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using System.Runtime.CompilerServices;

namespace Capstone.Identity.API.Services
{
    public class SkillTreeService : ISkillTreeService
    {
        private readonly IUserProfileRepository _userProfileRepo;
        private readonly IDisciplineRepository _disciplineRepository;
        private readonly ISkillRepository _skillRepository;
        private readonly IParticipantSkillProgressRepository _participantSkillProgressRepository;

        public SkillTreeService(IUserProfileRepository userProfileRepo, IDisciplineRepository disciplineRepository, ISkillRepository skillRepository, IParticipantSkillProgressRepository participantSkillProgressRepository)
        {
            _userProfileRepo = userProfileRepo;
            _disciplineRepository = disciplineRepository;
            _skillRepository = skillRepository;
            _participantSkillProgressRepository = participantSkillProgressRepository;
        }

        public async Task<Result<SkillTreeDto>> GetSkillTreeAsync(Guid userId)
        {
            var userProfile = await _userProfileRepo.GetUserProfileByUserIdAsync(userId);

            if (userProfile == null)
            {
                return Result<SkillTreeDto>.Failure("Participant not found.", ResultErrorType.NotFound);
            }

            var allActiveSkill = await _skillRepository.GetAllActiveSkills();
            var allDisciplines = await _disciplineRepository.GetAllDisciplinesAsync();
            var userProgressRows = await _participantSkillProgressRepository.GetForUserAsync(userProfile.UserProfileId);

            var progressRowsBySkillDict = new Dictionary<int, ParticipantSkillProgress>();

            foreach (var row in userProgressRows)
            {
                progressRowsBySkillDict.Add(row.SkillId, row);
            }

            var skillsByDisciplineDict = allActiveSkill
                .GroupBy(s => s.DisciplineId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var disciplineDtos = new List<SkillTreeDisciplineDto>();

            foreach (var discipline in allDisciplines.OrderBy(d => d.Name))
            {
                // Does this discipline have any active skills?
                if (!skillsByDisciplineDict.TryGetValue(discipline.DisciplineId, out var disciplineSkills))
                {
                    continue; // no entry = no active skills - hide it
                }

                var skillDtos = new List<SkillTreeSkillDto>();

                foreach (var skill in disciplineSkills.OrderBy(s => s.Name))
                {
                    // Does the participant have progress on this skill?
                    progressRowsBySkillDict.TryGetValue(skill.SkillId, out ParticipantSkillProgress? progress);

                    skillDtos.Add(skill.ToSkillTreeSkillDto(progress));
                }

                var skillTreeDisciplineDto = new SkillTreeDisciplineDto
                {
                    DisciplineId = discipline.DisciplineId,
                    Name = discipline.Name,
                    Skills = skillDtos
                };

                disciplineDtos.Add(skillTreeDisciplineDto);
            }

            var skillTreeDto = new SkillTreeDto
            {
                UserId = userId,
                Disciplines = disciplineDtos
            };

            return Result<SkillTreeDto>.Success(skillTreeDto);
        }

    }
}
