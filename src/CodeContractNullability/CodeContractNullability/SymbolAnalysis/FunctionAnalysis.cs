using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// General knowledge to support the symbol analysis process.
    /// </summary>
    internal static class FunctionAnalysis
    {
        private const MethodKind MethodKindLocalFunction = (MethodKind)17;

        public static readonly ImmutableArray<MethodKind> KindsToSkip =
            ImmutableArray.Create(MethodKind.AnonymousFunction, MethodKind.LambdaMethod, MethodKind.PropertyGet,
                MethodKind.PropertySet, MethodKindLocalFunction);
    }
}
