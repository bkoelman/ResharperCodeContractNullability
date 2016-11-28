using System.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.NullabilityAttributes
{
    /// <summary>
    /// Provides access to the (Item)NotNullAttribute and (Item)CanBeNullAttribute symbols in a compilation.
    /// </summary>
    public interface INullabilityAttributeProvider
    {
        [CanBeNull]
        NullabilityAttributeSymbols GetSymbols([NotNull] Compilation compilation,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
