using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeContractNullability.SymbolAnalysis;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;

namespace CodeContractNullability.Conversion
{
    internal sealed class NullConversionScope
    {
        [NotNull]
        private readonly NullConversionContext context;

        [NotNull]
        private readonly SyntaxNode declarationSyntax;

        [NotNull]
        private readonly ISymbol declarationSymbol;

        public NullConversionScope([NotNull] NullConversionContext context, [NotNull] SyntaxNode declarationSyntax, [NotNull] ISymbol declarationSymbol)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(declarationSyntax, nameof(declarationSyntax));
            Guard.NotNull(declarationSymbol, nameof(declarationSymbol));

            this.context = context;
            this.declarationSyntax = declarationSyntax;
            this.declarationSymbol = declarationSymbol;
        }

        public async Task RewriteDeclaration([NotNull] ResharperNullabilitySymbolState baseState)
        {
            Guard.NotNull(baseState, nameof(baseState));

            await RewriteDeclaration(baseState, false).ConfigureAwait(false);
        }

        private async Task RewriteDeclaration([NotNull] ResharperNullabilitySymbolState baseState, bool isInOtherPartOfMethod)
        {
            Guard.NotNull(baseState, nameof(baseState));

            DocumentEditor editor = await context.GetEditorForDeclaration(declarationSymbol).ConfigureAwait(false);

            ResharperNullabilitySymbolState nullabilityState = await ResharperNullabilityAttributesRemover
                .RemoveFromDeclaration(declarationSymbol, declarationSyntax, baseState, editor, context.CancellationToken).ConfigureAwait(false);

            nullabilityState = ReduceNullabilityState(nullabilityState);

            TypeSyntax declarationTypeSyntax = GetTypeSyntaxForDeclaration(declarationSyntax);

            editor.ReplaceNode(declarationTypeSyntax, (n, gen) => TypeDeclarationWriter.ToNullableTypeSyntax(n, nullabilityState));

            if (!(declarationSymbol is INamedTypeSymbol))
            {
                if (!isInOtherPartOfMethod)
                {
                    await ExecuteForPartialMethod(nullabilityState).ConfigureAwait(false);
                }

                await ExecuteForDerivedTypes(editor, nullabilityState).ConfigureAwait(false);
            }
        }

        [NotNull]
        private ResharperNullabilitySymbolState ReduceNullabilityState([NotNull] ResharperNullabilitySymbolState nullabilityState)
        {
            ITypeSymbol primaryDeclarationTypeSymbol = GetTypeFromSymbol(declarationSymbol);

            if (nullabilityState.PrimaryStatus != ResharperNullableStatus.Unspecified && !IsReferenceType(primaryDeclarationTypeSymbol))
            {
                nullabilityState = nullabilityState.ClearPrimaryStatus();
            }

            ITypeSymbol itemDeclarationTypeSymbol = primaryDeclarationTypeSymbol.TryGetItemTypeForSequenceOrCollection(context.TypeCache) ??
                primaryDeclarationTypeSymbol.TryGetItemTypeForLazyOrGenericTask(context.TypeCache);

            if (nullabilityState.ItemStatus != ResharperNullableStatus.Unspecified && !IsReferenceType(itemDeclarationTypeSymbol))
            {
                nullabilityState = nullabilityState.ClearItemStatus();
            }

            return nullabilityState;
        }

        private bool IsReferenceType([CanBeNull] ITypeSymbol typeSymbol)
        {
            return typeSymbol != null && !typeSymbol.IsValueType && !IsNullableValueType(typeSymbol);
        }

        private static bool IsNullableValueType([NotNull] ITypeSymbol declarationTypeSymbol)
        {
            return declarationTypeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
        }

        private async Task ExecuteForPartialMethod([NotNull] ResharperNullabilitySymbolState nullabilityState)
        {
            if (declarationSymbol is IParameterSymbol parameterSymbol && parameterSymbol.IsParameterInPartialMethod(context.CancellationToken))
            {
                var methodSymbol = (IMethodSymbol)parameterSymbol.ContainingSymbol;

                IMethodSymbol otherMethodSymbol = methodSymbol.PartialDefinitionPart ?? methodSymbol.PartialImplementationPart;

                if (otherMethodSymbol != null)
                {
                    int parameterIndex = methodSymbol.Parameters.IndexOf(parameterSymbol);
                    IParameterSymbol otherParameterSymbol = otherMethodSymbol.Parameters[parameterIndex];

                    SyntaxNode syntaxNode = otherParameterSymbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(context.CancellationToken))
                        .First();

                    var nextScope = new NullConversionScope(context, syntaxNode, otherParameterSymbol);
                    await nextScope.RewriteDeclaration(nullabilityState, true).ConfigureAwait(false);
                }
            }
        }

        private async Task ExecuteForDerivedTypes([NotNull] DocumentEditor editor, [NotNull] ResharperNullabilitySymbolState nullabilityState)
        {
            Solution solution = editor.OriginalDocument.Project.Solution;

            ISet<ISymbol> membersInDerivedTypes =
                await GetMembersInDerivedTypesAsync(declarationSymbol, solution, context.CancellationToken).ConfigureAwait(false);

            foreach (ISymbol memberInDerivedType in membersInDerivedTypes)
            {
                SyntaxNode syntaxNode = memberInDerivedType.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(context.CancellationToken))
                    .First();

                var nextScope = new NullConversionScope(context, syntaxNode, memberInDerivedType);
                await nextScope.RewriteDeclaration(nullabilityState).ConfigureAwait(false);
            }
        }

        [ItemNotNull]
        private static async Task<ISet<ISymbol>> GetMembersInDerivedTypesAsync([NotNull] ISymbol symbol, [NotNull] Solution solution,
            CancellationToken cancellationToken)
        {
            ISymbol memberSymbol = symbol is IParameterSymbol ? symbol.ContainingSymbol : symbol;

            var memberSet = new HashSet<ISymbol>();

            List<ISymbol> membersInDerivedType;

            if (symbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                IEnumerable<ISymbol> symbols = await SymbolFinder
                    .FindImplementationsAsync(memberSymbol, solution, null, cancellationToken).ConfigureAwait(false);

                membersInDerivedType = symbols.ToList();
            }
            else
            {
                IEnumerable<ISymbol> symbols = await SymbolFinder.FindOverridesAsync(memberSymbol, solution, null, cancellationToken).ConfigureAwait(false);

                membersInDerivedType = symbols.Where(x => symbol.ContainingType.Equals(x.ContainingType.BaseType)).ToList();
            }

            foreach (ISymbol memberInDerivedType in membersInDerivedType)
            {
                ISymbol memberInDerivedTypeCorrected = memberInDerivedType;

                if (symbol is IParameterSymbol parameterSymbol)
                {
                    memberInDerivedTypeCorrected = LookupParameterInDerivedTypeFor(parameterSymbol, memberInDerivedType);
                }

                memberSet.Add(memberInDerivedTypeCorrected);
            }

            return memberSet;
        }

        [NotNull]
        private static IParameterSymbol LookupParameterInDerivedTypeFor([NotNull] IParameterSymbol baseParameterSymbol,
            [NotNull] ISymbol derivedContainingSymbol)
        {
            int parameterIndex;

            if (baseParameterSymbol.ContainingSymbol is IMethodSymbol baseContainingMethod)
            {
                parameterIndex = baseContainingMethod.Parameters.IndexOf(baseParameterSymbol);

                var methodSymbol = (IMethodSymbol)derivedContainingSymbol;
                return methodSymbol.Parameters[parameterIndex];
            }

            if (baseParameterSymbol.ContainingSymbol is IPropertySymbol baseContainingProperty)
            {
                parameterIndex = baseContainingProperty.Parameters.IndexOf(baseParameterSymbol);

                var propertySymbol = (IPropertySymbol)derivedContainingSymbol;
                return propertySymbol.Parameters[parameterIndex];
            }

            throw new InvalidOperationException("Unspecified symbol type.");
        }

        [NotNull]
        private static TypeSyntax GetTypeSyntaxForDeclaration([NotNull] SyntaxNode declarationSyntax)
        {
            Guard.NotNull(declarationSyntax, nameof(declarationSyntax));

            switch (declarationSyntax)
            {
                case MethodDeclarationSyntax methodSyntax:
                {
                    return methodSyntax.ReturnType;
                }
                case OperatorDeclarationSyntax operatorSyntax:
                {
                    return operatorSyntax.ReturnType;
                }
                case ConversionOperatorDeclarationSyntax conversionOperatorSyntax:
                {
                    return conversionOperatorSyntax.Type;
                }
                case DelegateDeclarationSyntax delegateSyntax:
                {
                    return delegateSyntax.ReturnType;
                }
                case BasePropertyDeclarationSyntax propertySyntax:
                {
                    return propertySyntax.Type;
                }
                case BaseFieldDeclarationSyntax fieldSyntax:
                {
                    return fieldSyntax.Declaration.Type;
                }
                case ParameterSyntax parameterSyntax:
                {
                    return parameterSyntax.Type;
                }
            }

            throw new NotSupportedException($"Unexpected syntax of type '{declarationSyntax.GetType()}'.");
        }

        [NotNull]
        private static ITypeSymbol GetTypeFromSymbol([NotNull] ISymbol symbol)
        {
            Guard.NotNull(symbol, nameof(symbol));

            switch (symbol)
            {
                case IMethodSymbol methodSymbol:
                {
                    return methodSymbol.ReturnType;
                }
                case IPropertySymbol propertySymbol:
                {
                    return propertySymbol.Type;
                }
                case IParameterSymbol parameterSymbol:
                {
                    return parameterSymbol.Type;
                }
                case IFieldSymbol fieldSymbol:
                {
                    return fieldSymbol.Type;
                }
                case INamedTypeSymbol typeSymbol when typeSymbol.TypeKind == TypeKind.Delegate:
                {
                    return typeSymbol.DelegateInvokeMethod.ReturnType;
                }
            }

            throw new NotSupportedException($"Unexpected symbol of type '{symbol.GetType()}'.");
        }
    }
}
