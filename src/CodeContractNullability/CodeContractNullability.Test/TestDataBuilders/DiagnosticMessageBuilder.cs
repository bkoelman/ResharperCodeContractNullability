using System;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal sealed class DiagnosticMessageBuilder : ITestDataBuilder<string>
    {
        [CanBeNull]
        private SymbolType? symbolType;

        [CanBeNull]
        private string symbolName;

        private bool isItem;

        public string Build()
        {
            if (symbolType == null || symbolName == null)
            {
                throw new InvalidOperationException();
            }

            string nullability = isItem ? "item nullability" : "nullability";
            return $"{symbolType} '{symbolName}' is missing {nullability} annotation.";
        }

        [NotNull]
        public DiagnosticMessageBuilder OfType(SymbolType type)
        {
            symbolType = type;
            return this;
        }

        [NotNull]
        public DiagnosticMessageBuilder Named([CanBeNull] string name)
        {
            symbolName = name;
            return this;
        }

        [NotNull]
        public DiagnosticMessageBuilder ForItem()
        {
            isItem = true;
            return this;
        }
    }
}
