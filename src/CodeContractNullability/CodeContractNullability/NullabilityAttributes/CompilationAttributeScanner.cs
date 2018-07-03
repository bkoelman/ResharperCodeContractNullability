using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeContractNullability.SymbolAnalysis;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.NullabilityAttributes
{
    /// <summary>
    /// Scans through the source code and assembly references of a <see cref="Compilation" /> to locate the (Item)NotNullAttribute and
    /// (Item)CanBeNullAttribute types.
    /// </summary>
    internal sealed class CompilationAttributeScanner
    {
        private const string AttributeNameForNotNull = "NotNullAttribute";
        private const string AttributeNameForCanBeNull = "CanBeNullAttribute";
        private const string AttributeNameForItemNotNull = "ItemNotNullAttribute";
        private const string AttributeNameForItemCanBeNull = "ItemCanBeNullAttribute";
        private const string SystemAttributeTypeName = "System.Attribute";

        [CanBeNull]
        public NullabilityAttributeSymbols Scan([NotNull] Compilation compilation, CancellationToken cancellationToken)
        {
            Guard.NotNull(compilation, nameof(compilation));

            NullabilityAttributeSymbols result = ScanInSources(compilation, cancellationToken);
            return result ?? ScanInReferences(compilation, cancellationToken);
        }

        [CanBeNull]
        private NullabilityAttributeSymbols ScanInSources([NotNull] Compilation compilation, CancellationToken cancellationToken)
        {
            List<INamedTypeSymbol> matches = compilation.GetSymbolsWithName(IsAttributeName, SymbolFilter.Type, cancellationToken)
                .OfType<INamedTypeSymbol>().Where(x => IsUsableAttribute(x, true)).ToList();

            INamedTypeSymbol notNullAttributeSymbol = matches.FirstOrDefault(x => x.Name == AttributeNameForNotNull);
            INamedTypeSymbol canBeNullAttributeSymbol = matches.FirstOrDefault(x => x.Name == AttributeNameForCanBeNull);
            INamedTypeSymbol itemNotNullAttributeSymbol = matches.FirstOrDefault(x => x.Name == AttributeNameForItemNotNull);
            INamedTypeSymbol itemCanBeNullAttributeSymbol = matches.FirstOrDefault(x => x.Name == AttributeNameForItemCanBeNull);

            return notNullAttributeSymbol != null && canBeNullAttributeSymbol != null && itemNotNullAttributeSymbol != null &&
                itemCanBeNullAttributeSymbol != null
                    ? new NullabilityAttributeSymbols(notNullAttributeSymbol, canBeNullAttributeSymbol,
                        itemNotNullAttributeSymbol, itemCanBeNullAttributeSymbol)
                    : null;
        }

        private bool IsAttributeName([CanBeNull] string name)
        {
            return name == AttributeNameForNotNull || name == AttributeNameForCanBeNull || name == AttributeNameForItemNotNull ||
                name == AttributeNameForItemCanBeNull;
        }

        private static bool IsUsableAttribute([CanBeNull] INamedTypeSymbol symbol, bool allowInternal)
        {
            if (symbol?.BaseType != null && symbol.BaseType.GetFullMetadataName() == SystemAttributeTypeName)
            {
                INamedTypeSymbol container = symbol;
                while (container != null)
                {
                    if (container.DeclaredAccessibility != Accessibility.Public)
                    {
                        if (!allowInternal || container.DeclaredAccessibility != Accessibility.Internal)
                        {
                            return false;
                        }
                    }

                    container = container.ContainingType;
                }

                return true;
            }

            return false;
        }

        [CanBeNull]
        private NullabilityAttributeSymbols ScanInReferences([NotNull] Compilation compilation,
            CancellationToken cancellationToken)
        {
            foreach (MetadataReference reference in compilation.References)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol externalAssemblySymbol)
                {
                    var visitor = new NullabilityAttributesVisitor();
                    visitor.Visit(externalAssemblySymbol.GlobalNamespace);

                    if (visitor.NotNullAttributeSymbol != null && visitor.CanBeNullAttributeSymbol != null &&
                        visitor.ItemNotNullAttributeSymbol != null && visitor.ItemCanBeNullAttributeSymbol != null)
                    {
                        return new NullabilityAttributeSymbols(visitor.NotNullAttributeSymbol, visitor.CanBeNullAttributeSymbol,
                            visitor.ItemNotNullAttributeSymbol, visitor.ItemCanBeNullAttributeSymbol);
                    }
                }
            }

            return null;
        }

        private sealed class NullabilityAttributesVisitor : SymbolVisitor
        {
            [CanBeNull]
            public INamedTypeSymbol NotNullAttributeSymbol { get; private set; }

            [CanBeNull]
            public INamedTypeSymbol CanBeNullAttributeSymbol { get; private set; }

            [CanBeNull]
            public INamedTypeSymbol ItemNotNullAttributeSymbol { get; private set; }

            [CanBeNull]
            public INamedTypeSymbol ItemCanBeNullAttributeSymbol { get; private set; }

            private bool IsComplete =>
                NotNullAttributeSymbol != null && CanBeNullAttributeSymbol != null && ItemNotNullAttributeSymbol != null &&
                ItemCanBeNullAttributeSymbol != null;

            public override void VisitNamespace([NotNull] INamespaceSymbol symbol)
            {
                if (!IsComplete)
                {
                    foreach (INamespaceOrTypeSymbol member in symbol.GetMembers())
                    {
                        member.Accept(this);

                        if (IsComplete)
                        {
                            return;
                        }
                    }
                }
            }

            public override void VisitNamedType([NotNull] INamedTypeSymbol symbol)
            {
                if (symbol.Name == AttributeNameForNotNull)
                {
                    if (IsUsableAttribute(symbol, false))
                    {
                        NotNullAttributeSymbol = symbol;
                    }
                }
                else if (symbol.Name == AttributeNameForCanBeNull)
                {
                    if (IsUsableAttribute(symbol, false))
                    {
                        CanBeNullAttributeSymbol = symbol;
                    }
                }
                else if (symbol.Name == AttributeNameForItemNotNull)
                {
                    if (IsUsableAttribute(symbol, false))
                    {
                        ItemNotNullAttributeSymbol = symbol;
                    }
                }
                else if (symbol.Name == AttributeNameForItemCanBeNull)
                {
                    if (IsUsableAttribute(symbol, false))
                    {
                        ItemCanBeNullAttributeSymbol = symbol;
                    }
                }
            }
        }
    }
}
