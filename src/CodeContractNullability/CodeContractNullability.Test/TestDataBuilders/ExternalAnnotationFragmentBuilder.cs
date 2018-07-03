using System.Collections.Generic;
using System.Xml.Linq;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal sealed class ExternalAnnotationFragmentBuilder : ITestDataBuilder<XElement>
    {
        /* Example output:
            <member name=""M:TestSystem.StringFormatter.Format(System.IFormatProvider,System.String,System.Object[])"">
                <attribute ctor=""M:JetBrains.Annotations.NotNullAttribute.#ctor"" />
                <parameter name=""provider"">
                    <attribute ctor=""M:JetBrains.Annotations.CanBeNullAttribute.#ctor"" />
                </parameter>
                <parameter name=""format"">
                    <attribute ctor=""M:JetBrains.Annotations.CanBeNullAttribute.#ctor"" />
                </parameter>
                <parameter name=""args"">
                    <attribute ctor=""M:JetBrains.Annotations.CanBeNullAttribute.#ctor"" />
                </parameter>
            </member>
        */

        [NotNull]
        private string memberName = "value";

        [CanBeNull]
        private bool? isNotNull;

        [NotNull]
        [ItemNotNull]
        private readonly List<XElement> parameters = new List<XElement>();

        public XElement Build()
        {
            var element = new XElement("member", new XAttribute("name", memberName));

            if (isNotNull != null)
            {
                element.Add(new XElement("attribute",
                    new XAttribute("ctor",
                        isNotNull.Value
                            ? "M:JetBrains.Annotations.NotNullAttribute.#ctor"
                            : "M:JetBrains.Annotations.CanBeNullAttribute.#ctor")));
            }

            foreach (XElement parameter in parameters)
            {
                element.Add(parameter);
            }

            return element;
        }

        [NotNull]
        public ExternalAnnotationFragmentBuilder Named([NotNull] string name)
        {
            Guard.NotNull(name, nameof(name));

            memberName = name;
            return this;
        }

        [NotNull]
        public ExternalAnnotationFragmentBuilder NotNull()
        {
            isNotNull = true;
            return this;
        }

        [NotNull]
        public ExternalAnnotationFragmentBuilder CanBeNull()
        {
            isNotNull = false;
            return this;
        }

        [NotNull]
        public ExternalAnnotationFragmentBuilder WithParameter([NotNull] ExternalAnnotationParameterBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));

            XElement parameter = builder.Build();
            parameters.Add(parameter);
            return this;
        }
    }
}
