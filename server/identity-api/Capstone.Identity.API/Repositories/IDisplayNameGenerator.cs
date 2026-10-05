using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Repositories
{
    public interface IDisplayNameGenerator
    {
        DisplayNameSalutations DefaultSalutation { get; }
        DisplayNameModifiers PickRandomModifier();
        public Task<string> GenerateDisplayName(DisplayNameSalutations salutation, string firstName, DisplayNameModifiers modifier);
    }
}
