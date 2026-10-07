namespace Capstone.Identity.API.Models.ReadModels
{
    /// <summary>
    /// All of this information is required for admin view of 
    /// users behind the scenes
    /// </summary>
    /// <param name="user"></param>
    /// <param name="profile"></param>
    /// <param name="Roles"></param>
    public sealed record UserWithProfile(
        ApplicationUser User,
        UserProfile Profile,
        IReadOnlyList<string> Roles);
}
