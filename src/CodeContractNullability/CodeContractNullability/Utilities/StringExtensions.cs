using JetBrains.Annotations;

namespace CodeContractNullability.Utilities
{
    internal static class StringExtensions
    {
        [NotNull]
        public static string ToCamelCase([NotNull] this string memberTypePascalCase)
        {
            Guard.NotNullNorWhiteSpace(memberTypePascalCase, nameof(memberTypePascalCase));

            string firstChar = memberTypePascalCase.Substring(0, 1).ToLowerInvariant();
            return memberTypePascalCase.Length > 1 ? firstChar + memberTypePascalCase.Substring(1) : firstChar;
        }
    }
}
