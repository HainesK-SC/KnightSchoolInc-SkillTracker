using Capstone.Identity.API.Enums;
using Microsoft.Identity.Client;

namespace Capstone.Identity.API.Models
{
    public static class ApplicationUserExtensions
    {
        public static AccountStatus GetAccountStatus(this ApplicationUser user)
        {
            var accountStatus = new AccountStatus();

            if (user.PasswordHash is not null)
            {
                return accountStatus = AccountStatus.Active;
            }

            if (user.Email is not null)
            {
                return accountStatus = AccountStatus.PendingActiviation;
            }

            return accountStatus = AccountStatus.WalkUp;
        }
    }
}
