using System.Collections.Generic;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public class NullabilityAttributesDefinition
    {
        [NotNull]
        public string CodeNamespace { get; }

        [NotNull]
        [ItemNotNull]
        public IList<string> NestedTypes { get; }

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

                    foreach (string nestedType in NestedTypes)
                    {
                        textBuilder.AppendLine(nestedType);
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

                    for (int index = 0; index < NestedTypes.Count; index++)
                    {
                        textBuilder.AppendLine("}");
                    }

                    if (!string.IsNullOrEmpty(CodeNamespace))
                    {
                        textBuilder.AppendLine("}");
                    }
                }
                return textBuilder.ToString();
            }
        }

        public NullabilityAttributesDefinition([NotNull] string codeNamespace,
            [NotNull] [ItemNotNull] IList<string> nestedTypes)
        {
            Guard.NotNull(codeNamespace, nameof(codeNamespace));

            CodeNamespace = codeNamespace;
            NestedTypes = nestedTypes;
        }
    }
}