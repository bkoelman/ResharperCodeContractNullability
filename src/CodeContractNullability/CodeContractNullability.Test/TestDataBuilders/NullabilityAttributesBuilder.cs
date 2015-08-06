using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public class NullabilityAttributesBuilder : ITestDataBuilder<NullabilityAttributesDefinition>
    {
        [NotNull]
        private string codeNamespace = "Namespace.For.JetBrains.Annotation.Attributes";

        private bool imported;

        public NullabilityAttributesDefinition Build()
        {
            return new NullabilityAttributesDefinition(codeNamespace, imported);
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
    }
}