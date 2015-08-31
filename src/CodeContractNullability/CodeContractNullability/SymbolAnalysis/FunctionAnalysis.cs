using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.SymbolAnalysis
{
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