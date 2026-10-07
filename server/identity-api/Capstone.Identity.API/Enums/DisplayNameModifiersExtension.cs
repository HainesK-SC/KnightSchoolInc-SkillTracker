using System.Runtime.CompilerServices;

namespace Capstone.Identity.API.Enums
{
    public static class DisplayNameModifiersExtension
    {
        public static string ToDisplayText(this DisplayNameModifiers modifier) => modifier switch
        {
            DisplayNameModifiers.TheBrave => "the Brave",
            DisplayNameModifiers.TheBold => "the Bold",
            DisplayNameModifiers.TheWise => "the Wise",
            DisplayNameModifiers.TheSwift => "the Swift",
            DisplayNameModifiers.TheSteadfast => "the Steadfast",
            DisplayNameModifiers.TheValiant => "the Valiant",
            _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null)
        };
    }
}
