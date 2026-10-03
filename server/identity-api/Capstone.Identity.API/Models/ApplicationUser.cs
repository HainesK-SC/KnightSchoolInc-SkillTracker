using Microsoft.AspNetCore.Identity;
using System.Runtime.CompilerServices;

namespace Capstone.Identity.API.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
