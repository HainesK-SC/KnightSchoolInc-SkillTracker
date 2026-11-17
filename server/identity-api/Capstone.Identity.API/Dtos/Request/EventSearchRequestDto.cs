using System.ComponentModel.DataAnnotations;

namespace Capstone.Identity.API.Dtos.Request
{
    public class EventSearchRequestDto
    {
        [MaxLength(100)]
        public string? Search { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public bool IncludeCancelled { get; set; } = false;
    }
}
