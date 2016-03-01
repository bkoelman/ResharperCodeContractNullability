using CodeContractNullability.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability
{
    /// <summary>
    /// Entry point for analyzer that creates diagnostics for members that need item nullability annotation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CodeContractItemNullabilityAnalyzer : BaseAnalyzer
    {
        public const string DiagnosticId = "RINUL";

        public CodeContractItemNullabilityAnalyzer()
            : base(true)
        {
        }

        protected override DiagnosticDescriptor CreateRuleFor(string memberTypePascalCase)
        {
            string title = $"{memberTypePascalCase} is missing item nullability annotation.";
            string messageFormat = $"{memberTypePascalCase} '{{0}}' is missing item nullability annotation.";
            string description =
                $"The item type of this sequence/collection {memberTypePascalCase.ToCamelCase()} is a reference type or nullable type; it should be annotated with [ItemNotNull] or [ItemCanBeNull].";

            return new DiagnosticDescriptor(DiagnosticId, title, messageFormat, Category, DiagnosticSeverity.Warning,
                true, description,
                "https://github.com/bkoelman/ResharperCodeContractNullability/blob/master/doc/reference/RINUL_MemberIsMissingItemNullabilityAnnotation.md");
        }
    }
}