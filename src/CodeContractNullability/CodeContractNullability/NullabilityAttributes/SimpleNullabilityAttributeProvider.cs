using System.Threading;
using CodeContractNullability.Utilities;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.NullabilityAttributes
{
    /// <summary>
    /// Provides direct access to the (Item)NotNullAttribute and (Item)CanBeNullAttribute symbols in a compilation.
    /// </summary>
    public sealed class SimpleNullabilityAttributeProvider : INullabilityAttributeProvider
    {
        public NullabilityAttributeSymbols GetSymbols(Compilation compilation,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(compilation, nameof(compilation));

            var scanner = new CompilationAttributeScanner();
            return scanner.Scan(compilation, cancellationToken);
        }
    }
}
