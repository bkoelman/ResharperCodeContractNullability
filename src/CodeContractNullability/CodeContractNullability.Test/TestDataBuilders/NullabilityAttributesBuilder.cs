using System.Collections.Generic;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public class NullabilityAttributesBuilder : ITestDataBuilder<NullabilityAttributesDefinition>
    {
        [NotNull]
        private string codeNamespace = "Namespace.For.JetBrains.Annotation.Attributes";

        private bool imported;
        private readonly List<string> nestedTypes = new List<string>();

        public NullabilityAttributesDefinition Build()
        {
            return new NullabilityAttributesDefinition(codeNamespace, nestedTypes, imported);
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
        public NullabilityAttributesBuilder Imported()
        {
            imported = true;
            return this;
        }

        public NullabilityAttributesBuilder NestedInTypes([NotNull] IEnumerable<string> scopes)
        {
            Guard.NotNull(scopes, nameof(scopes));

            nestedTypes.AddRange(scopes);
            return this;
        }
    }
}