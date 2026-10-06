using Capstone.Identity.API.Enums;

namespace Capstone.Identity.API.Services
{
    public sealed class DisplayNameGenerator : IDisplayNameGenerator
    {
        private static readonly DisplayNameModifiers[] Modifiers = Enum.GetValues<DisplayNameModifiers>();

        public DisplayNameSalutations DefaultSalutation => DisplayNameSalutations.Knight;

        public DisplayNameModifiers PickRandomModifier() =>
            Modifiers[Random.Shared.Next(Modifiers.Length)];

        public string GenerateDisplayName(DisplayNameSalutations salutation, string firstName, DisplayNameModifiers modifier)
        {
            var displayName = $"{salutation.ToDisplayText()}, {firstName.Trim()} {modifier.ToDisplayText()}";

            return displayName;
        }
        
    }
}
