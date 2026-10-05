namespace Capstone.Identity.API.Repositories
{
    public interface IDisplayNameGenerator
    {
        public Task<string> GenerateDisplayName(string firstName);
    }
}
