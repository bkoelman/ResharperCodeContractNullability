using System;
using System.Xml.Linq;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal sealed class ExternalAnnotationParameterBuilder : ITestDataBuilder<XElement>
    {
        [CanBeNull]
        private bool? isNotNull;

        [NotNull]
        private string parameterName = "value";

        public XElement Build()
        {
            if (isNotNull == null)
            {
                throw new InvalidOperationException("Nullability must be set explicitly.");
            }

            return new XElement("parameter", new XAttribute("name", parameterName),
                new XElement("attribute",
                    new XAttribute("ctor",
                        isNotNull.Value
                            ? "M:JetBrains.Annotations.NotNullAttribute.#ctor"
                            : "M:JetBrains.Annotations.CanBeNullAttribute.#ctor")));
        }

        [NotNull]
        public ExternalAnnotationParameterBuilder Named([NotNull] string name)
        {
            Guard.NotNull(name, nameof(name));

            parameterName = name;
            return this;
        }

        [NotNull]
        public ExternalAnnotationParameterBuilder NotNull()
        {
            isNotNull = true;
            return this;
        }

        [NotNull]
        public ExternalAnnotationParameterBuilder CanBeNull()
        {
            isNotNull = false;
            return this;
        }
    }
}
