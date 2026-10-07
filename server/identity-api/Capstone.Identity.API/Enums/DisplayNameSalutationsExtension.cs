using System.Runtime.CompilerServices;

namespace Capstone.Identity.API.Enums
{
    public static class DisplayNameSalutationsExtension
    {
        public static string ToDisplayText(this DisplayNameSalutations salutation) => salutation switch
        {
            DisplayNameSalutations.Knight => "Knight",
            DisplayNameSalutations.Sir => "Sir",
            DisplayNameSalutations.Madame => "Madame",
            _ => throw new ArgumentOutOfRangeException(nameof(salutation), salutation, null)
        };
    }
}
