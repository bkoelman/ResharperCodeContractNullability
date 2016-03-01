using CodeContractNullability.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability
{
    /// <summary>
    /// Entry point for analyzer that creates diagnostics for members that need nullability annotation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CodeContractNullabilityAnalyzer : BaseAnalyzer
    {
        public const string DiagnosticId = "RNUL";

        public CodeContractNullabilityAnalyzer()
            : base(false)
        {
        }

        protected override DiagnosticDescriptor CreateRuleFor(string memberTypePascalCase)
        {
            string title = $"{memberTypePascalCase} is missing nullability annotation.";
            string messageFormat = $"{memberTypePascalCase} '{{0}}' is missing nullability annotation.";
            string description =
                $"The type of this {memberTypePascalCase.ToCamelCase()} is a reference type or nullable type; it should be annotated with [NotNull] or [CanBeNull].";

            return new DiagnosticDescriptor(DiagnosticId, title, messageFormat, Category, DiagnosticSeverity.Warning,
                true, description,
                "https://github.com/bkoelman/ResharperCodeContractNullability/blob/master/doc/reference/RNUL_MemberIsMissingNullabilityAnnotation.md");
        }
    }
}