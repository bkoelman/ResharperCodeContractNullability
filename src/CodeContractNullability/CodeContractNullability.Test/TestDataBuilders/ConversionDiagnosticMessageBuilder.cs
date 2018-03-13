using System;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal sealed class ConversionDiagnosticMessageBuilder : ITestDataBuilder<string>
    {
        [CanBeNull]
        private SymbolType? symbolType;

        [CanBeNull]
        private string symbolName;

        public string Build()
        {
            if (symbolType == null || symbolName == null)
            {
                throw new InvalidOperationException("Symbol type or name must be set.");
            }

            string typeName = symbolType.ToString().ToLowerInvariant();
            return $"Resharper nullability annotation(s) on {typeName} '{symbolName}' can be converted to C# syntax.";
        }

        [NotNull]
        public ConversionDiagnosticMessageBuilder OfType(SymbolType type)
        {
            symbolType = type;
            return this;
        }

        [NotNull]
        public ConversionDiagnosticMessageBuilder Named([CanBeNull] string name)
        {
            symbolName = name;
            return this;
        }
    }
}
