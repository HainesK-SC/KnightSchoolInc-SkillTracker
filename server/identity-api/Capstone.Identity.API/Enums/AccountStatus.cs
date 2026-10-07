namespace Capstone.Identity.API.Enums
{
    /// <summary>
    /// The purpose of this enum is to identify an account status.
    /// There are placeholder accounts that are created for event participants
    /// of whom do not have an account in the application and will not register
    /// for an account prior to enrollment. This can leave accounts in one of 
    /// three states:
    ///   1. WalkUp - no email, no password - cannot login (email can be assigned manually by admin staff)
    ///   2. PendingActivation - walkup event enrollment with email provided, no password - awaiting password
    ///   3. Active - full account
    /// </summary>
    public enum AccountStatus
    {
        WalkUp, // no email, no password - can't login
        PendingActiviation, // email, no password - awaiting password to be set
        Active // full user account
    }
}
