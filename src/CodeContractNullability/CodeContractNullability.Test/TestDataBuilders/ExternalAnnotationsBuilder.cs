using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal sealed class ExternalAnnotationsBuilder : ITestDataBuilder<ExternalAnnotationsMap>
    {
        [NotNull]
        [ItemNotNull]
        private readonly List<XElement> fragments = new List<XElement>();

        public ExternalAnnotationsMap Build()
        {
            if (fragments.Any())
            {
                string document = GetDocument();
                return Parse(document);
            }

            return new ExternalAnnotationsMap();
        }

        [NotNull]
        private string GetDocument()
        {
            var textBuilder = new StringBuilder();
            textBuilder.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>
                <assembly name=""System.ThisValueIsNotUsed, Version=1.0.0.0"">");

            foreach (XElement fragment in fragments)
            {
                textBuilder.AppendLine(fragment.ToString());
            }

            textBuilder.AppendLine("</assembly>");
            return textBuilder.ToString();
        }

        [NotNull]
        private static ExternalAnnotationsMap Parse([NotNull] string document)
        {
            var annotations = new ExternalAnnotationsMap();

            var parser = new ExternalAnnotationDocumentParser();
            using (var reader = new StringReader(document))
            {
                parser.ProcessDocument(reader, annotations);
            }

            return annotations;
        }

        [NotNull]
        public ExternalAnnotationsBuilder IncludingMember([NotNull] ExternalAnnotationFragmentBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));

            XElement fragment = builder.Build();
            fragments.Add(fragment);
            return this;
        }
    }
}
