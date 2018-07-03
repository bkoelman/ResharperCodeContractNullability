using JetBrains.Annotations;

namespace CodeContractNullability.Utilities
{
    internal static class StringExtensions
    {
        [NotNull]
        public static string ToCamelCase([NotNull] this string memberTypePascalCase)
        {
            Guard.NotNull(memberTypePascalCase, nameof(memberTypePascalCase));

            return memberTypePascalCase.Length > 1
                ? memberTypePascalCase.Substring(0, 1).ToLowerInvariant() + memberTypePascalCase.Substring(1)
                : memberTypePascalCase;
        }
    }
}
