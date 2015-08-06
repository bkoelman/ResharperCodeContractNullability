using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.SymbolAnalysis
{
    internal static class MemberAnalyzer
    {
        public static readonly ImmutableArray<MethodKind> MethodKindsToSkip =
            ImmutableArray.Create(new[]
            {
                MethodKind.AnonymousFunction,
                MethodKind.LambdaMethod,
                MethodKind.PropertyGet,
                MethodKind.PropertySet
            });
    }
}