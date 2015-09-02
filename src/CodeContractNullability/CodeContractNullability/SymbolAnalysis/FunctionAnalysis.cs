using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// General knowledge to support the symbol analysis process.
    /// </summary>
    internal static class FunctionAnalysis
    {
        public static readonly ImmutableArray<MethodKind> KindsToSkip =
            ImmutableArray.Create(new[]
            {
                MethodKind.AnonymousFunction,
                MethodKind.LambdaMethod,
                MethodKind.PropertyGet,
                MethodKind.PropertySet
            });
    }
}