using Capstone.Identity.API.Dtos.Admin;
using Capstone.Identity.API.Models.ReadModels;

namespace Capstone.Identity.API.Models
{
    public static class UserProfileExtensions
    {
        public static AdminUserDto ToAdminUserDto(this UserWithProfile row)
        {
            return new AdminUserDto
            {
                Id = row.User.Id,
                FirstName = row.User.FirstName,
                LastName = row.User.LastName,
                Email = row.User.Email,
                Salutation = row.Profile.DisplayNameSalutation,
                Modifier = row.Profile.DisplayNameModifier,
                DisplayName = row.Profile.DisplayName,
                AvatarImagePath = row.Profile.AvatarImagePath,
                Status = row.User.GetAccountStatus(),
                Roles = row.Roles
            };
        }
    }
}
