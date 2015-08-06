using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public class NullabilityAttributesDefinition
    {
        [NotNull]
        public string CodeNamespace { get; }

        public bool Imported { get; }

        [NotNull]
        public string SourceText
        {
            get
            {
                var textBuilder = new StringBuilder();
                if (true)
                {
                    if (!string.IsNullOrEmpty(CodeNamespace))
                    {
                        textBuilder.AppendLine();
                        textBuilder.AppendLine("namespace " + CodeNamespace);
                        textBuilder.AppendLine("{");
                    }

                    textBuilder.AppendLine(@"
    [System.AttributeUsage(
        System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property |
        System.AttributeTargets.Delegate | System.AttributeTargets.Field | System.AttributeTargets.Event)]
    [System.Diagnostics.Conditional(""JETBRAINS_ANNOTATIONS"")]
    public sealed class CanBeNullAttribute : System.Attribute { }

    [System.AttributeUsage(
        System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property |
        System.AttributeTargets.Delegate | System.AttributeTargets.Field | System.AttributeTargets.Event)]
    [System.Diagnostics.Conditional(""JETBRAINS_ANNOTATIONS"")]
    public sealed class NotNullAttribute : System.Attribute { }

    [System.AttributeUsage(
        System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property |
        System.AttributeTargets.Delegate | System.AttributeTargets.Field)]
    [System.Diagnostics.Conditional(""JETBRAINS_ANNOTATIONS"")]
    public sealed class ItemNotNullAttribute : System.Attribute { }

    [System.AttributeUsage(
        System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property |
        System.AttributeTargets.Delegate | System.AttributeTargets.Field)]
    [System.Diagnostics.Conditional(""JETBRAINS_ANNOTATIONS"")]
    public sealed class ItemCanBeNullAttribute : System.Attribute { }
");
                    if (!string.IsNullOrEmpty(CodeNamespace))
                    {
                        textBuilder.AppendLine("}");
                    }
                }
                return textBuilder.ToString();
            }
        }

        public NullabilityAttributesDefinition([NotNull] string codeNamespace, bool imported)
        {
            Guard.NotNull(codeNamespace, nameof(codeNamespace));

            CodeNamespace = codeNamespace;
            Imported = imported;
        }
    }
}