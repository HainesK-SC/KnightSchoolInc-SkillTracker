using System.Runtime.CompilerServices;

namespace Capstone.Identity.API.Enums
{
    public static class SkillStatusExtensions
    {
        public static string ToDisplayText(this SkillStatus skillStatus) => skillStatus switch
        {
            SkillStatus.InProgress => "In Progress",
            SkillStatus.Completed => "Completed",
            SkillStatus.Locked => "Locked",
            _ => throw new ArgumentOutOfRangeException(nameof(skillStatus), skillStatus, null)
        };
    }
}
