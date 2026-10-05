using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Repositories;

namespace Capstone.Identity.API.Services
{
    public sealed class DisplayNameGenerator : IDisplayNameGenerator
    {
        private static readonly DisplayNameModifiers[] Modifiers = Enum.GetValues<DisplayNameModifiers>();

        public DisplayNameSalutations DefaultSalutation => DisplayNameSalutations.Knight;

        public DisplayNameModifiers PickRandomModifier() =>
            Modifiers[Random.Shared.Next(Modifiers.Length)];

        public async Task<string> GenerateDisplayName(DisplayNameSalutations salutation, string firstName, DisplayNameModifiers modifier) =>
            $"{salutation.ToDisplayText()}, {firstName.Trim()} {modifier.ToDisplayText()}";
    }
}
