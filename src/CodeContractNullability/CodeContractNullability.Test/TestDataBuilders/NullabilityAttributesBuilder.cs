using System.Collections.Generic;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal sealed class NullabilityAttributesBuilder : ITestDataBuilder<NullabilityAttributesDefinition>
    {
        [NotNull]
        private string codeNamespace = "Namespace.For.JetBrains.Annotation.Attributes";

        [NotNull]
        [ItemNotNull]
        private readonly List<string> nestedTypes = new List<string>();

        public NullabilityAttributesDefinition Build()
        {
            return new NullabilityAttributesDefinition(codeNamespace, nestedTypes);
        }

        [NotNull]
        public NullabilityAttributesBuilder InCodeNamespace([NotNull] string ns)
        {
            Guard.NotNull(ns, nameof(ns));

            codeNamespace = ns;
            return this;
        }

        [NotNull]
        public NullabilityAttributesBuilder InGlobalNamespace()
        {
            return InCodeNamespace(string.Empty);
        }

        [NotNull]
        public NullabilityAttributesBuilder NestedInTypes([NotNull] [ItemNotNull] IEnumerable<string> scopes)
        {
            Guard.NotNull(scopes, nameof(scopes));

            nestedTypes.AddRange(scopes);
            return this;
        }
    }
}
