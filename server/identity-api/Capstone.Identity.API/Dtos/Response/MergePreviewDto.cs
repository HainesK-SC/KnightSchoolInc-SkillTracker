using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Dtos.Response
{
    public class MergePreviewDto
    {
        public MergeAccountSummaryDto Placeholder { get; set; } = new();
        public MergeAccountSummaryDto Target { get; set; } = new();
        public int ProgressToMove { get; set; }
        public List<MergeProgressConflictDto> ProgressConflicts { get; set; } = [];
        public int EnrollmentsToMove { get; set; }
        public int DuplicateEnrollmentsDropped { get; set; }
        public List<int> OverlappingEventIds { get; set; } = [];
        public bool CanMerge { get; set; }
    }

    public class MergeAccountSummaryDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
    }

    public class MergeProgressConflictDto
    {
        public int SkillId { get; set; }
        public SkillStatus PlaceholderStatus { get; set; }
        public SkillStatus TargetStatus { get; set; }
        public SkillStatus KeptStatus { get; set; }
    }
}