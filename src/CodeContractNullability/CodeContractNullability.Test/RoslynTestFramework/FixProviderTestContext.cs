using System.Collections.Generic;
using System.Collections.Immutable;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.RoslynTestFramework
{
    public sealed class FixProviderTestContext
    {
        [NotNull]
        public AnalyzerTestContext AnalyzerTestContext { get; }

        [NotNull]
        [ItemNotNull]
        public ImmutableList<string> Expected { get; }

        public bool ReformatExpected { get; }

        public FixProviderTestContext([NotNull] AnalyzerTestContext analyzerTestContext,
            [NotNull] [ItemNotNull] IEnumerable<string> expected, bool reformatExpected = true)
        {
            Guard.NotNull(analyzerTestContext, nameof(analyzerTestContext));
            Guard.NotNull(expected, nameof(expected));

            AnalyzerTestContext = analyzerTestContext;
            Expected = ImmutableList.CreateRange(expected);
            ReformatExpected = reformatExpected;
        }

        [NotNull]
        public FixProviderTestContext WithExpected([NotNull] [ItemNotNull] IEnumerable<string> expected)
        {
            Guard.NotNull(expected, nameof(expected));

            return new FixProviderTestContext(AnalyzerTestContext, expected, ReformatExpected);
        }
    }
}