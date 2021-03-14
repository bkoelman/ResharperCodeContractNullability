using System.Collections.Generic;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    /// <summary />
    internal sealed class TypeSourceCodeBuilder : SourceCodeBuilder
    {
        [NotNull]
        [ItemNotNull]
        private readonly List<string> types = new();

        protected override string GetSourceCode()
        {
            var builder = new StringBuilder();

            AppendTypes(builder);

            return builder.ToString();
        }

        private void AppendTypes([NotNull] StringBuilder builder)
        {
            string code = GetLinesOfCode(types);
            builder.AppendLine(code);
        }

        [NotNull]
        public TypeSourceCodeBuilder ClearGlobalScope()
        {
            types.Clear();
            return this;
        }

        [NotNull]
        public TypeSourceCodeBuilder InGlobalScope([NotNull] string typeCode)
        {
            Guard.NotNull(typeCode, nameof(typeCode));

            types.Add(typeCode);
            return this;
        }
    }
}
