using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public sealed class ClassSourceCodeBuilder : SourceCodeBuilder
    {
        [NotNull]
        [ItemNotNull]
        private readonly List<string> classes = new List<string>();

        private bool generateNamespace;

        protected override string GetSourceCode()
        {
            var builder = new StringBuilder();

            if (generateNamespace)
            {
                builder.AppendLine("namespace TestNamespace");
                builder.AppendLine("{");
            }

            string code = GetLinesOfCode(classes);
            builder.AppendLine(code);

            if (generateNamespace)
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        [NotNull]
        public ClassSourceCodeBuilder InGlobalScope([NotNull] string classCode)
        {
            Guard.NotNull(classCode, nameof(classCode));

            classes.Add(classCode);
            generateNamespace = false;
            return this;
        }
    }
}
