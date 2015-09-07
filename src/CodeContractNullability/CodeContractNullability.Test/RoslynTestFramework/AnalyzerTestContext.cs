using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.Test.RoslynTestFramework
{
    public sealed class AnalyzerTestContext
    {
        private const string DefaultFileName = "TestDocument";

        [NotNull]
        [ItemNotNull]
        private static readonly ImmutableList<MetadataReference> DefaultReferences =
            ImmutableList.Create(new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof (object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof (Enumerable).GetTypeInfo().Assembly.Location)
            });

        [NotNull]
        public string MarkupCode { get; }

        [NotNull]
        public string LanguageName { get; }

        [NotNull]
        public string FileName { get; }

        [NotNull]
        [ItemNotNull]
        public ImmutableList<MetadataReference> References { get; }

        private AnalyzerTestContext([NotNull] string markupCode, [NotNull] string languageName,
            [NotNull] string fileName, [NotNull] [ItemNotNull] ImmutableList<MetadataReference> references)
        {
            MarkupCode = markupCode;
            LanguageName = languageName;
            FileName = fileName;
            References = references;
        }

        public AnalyzerTestContext([NotNull] string markupCode, [NotNull] string languageName)
            : this(markupCode, languageName, DefaultFileName, DefaultReferences)
        {
            Guard.NotNull(markupCode, nameof(markupCode));
            Guard.NotNull(languageName, nameof(languageName));
        }

        [NotNull]
        public AnalyzerTestContext WithFileName([NotNull] string fileName)
        {
            Guard.NotNull(fileName, nameof(fileName));

            return new AnalyzerTestContext(MarkupCode, LanguageName, fileName, References);
        }

        [NotNull]
        public AnalyzerTestContext WithReferences([NotNull] [ItemNotNull] IEnumerable<MetadataReference> references)
        {
            Guard.NotNull(references, nameof(references));

            ImmutableList<MetadataReference> referenceList = ImmutableList.CreateRange(references);
            return new AnalyzerTestContext(MarkupCode, LanguageName, FileName, referenceList);
        }
    }
}