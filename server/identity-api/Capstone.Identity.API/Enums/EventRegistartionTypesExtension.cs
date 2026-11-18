using System.Runtime.CompilerServices;

namespace Capstone.Identity.API.Enums
{
    public static class EventRegistartionTypesExtension
    {
        public static string ToDisplayText(this EventRegistrationType registrationType) => registrationType switch
        {
            EventRegistrationType.PRIVATE => "Private",
            EventRegistrationType.PUBLIC => "Public",
            _ => throw new ArgumentOutOfRangeException(nameof(registrationType), registrationType, null)
        };
    }
}
