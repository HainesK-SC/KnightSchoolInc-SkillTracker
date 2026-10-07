namespace Capstone.Identity.API.Models
{
    public static class Roles
    {
        public const string Admin = "ADMINISTRATOR";
        public const string RegularUser = "REGULAR_USER";
        public const string Instructor = "INSTRUCTOR";

        public async static Task<List<string>> GetAllRolesAsync()
        {
            var roles = new List<string>
            {
                Admin,
                RegularUser,
                Instructor
            };

            return roles;
        }
    }
}
