using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeContractNullability
{
    /// <summary />
    internal static class SyntaxNodeExtensions
    {
        [NotNull]
        public static SyntaxNode TranslateDeclarationSyntax([NotNull] this SyntaxNode syntax)
        {
            if (syntax is VariableDeclaratorSyntax fieldVariableSyntax)
            {
                return fieldVariableSyntax.GetAncestorOrThis<FieldDeclarationSyntax>();
            }

            if (syntax is IdentifierNameSyntax identifierNameSyntax &&
                identifierNameSyntax.Parent is ConversionOperatorDeclarationSyntax conversionOperatorDeclarationSyntax)
            {
                return conversionOperatorDeclarationSyntax;
            }

            return syntax;
        }

        [CanBeNull]
        private static TNode GetAncestorOrThis<TNode>([CanBeNull] this SyntaxNode node)
            where TNode : SyntaxNode
        {
            return GetAncestorsOrThis<TNode>(node).FirstOrDefault();
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<TNode> GetAncestorsOrThis<TNode>([CanBeNull] this SyntaxNode node)
            where TNode : SyntaxNode
        {
            return node?.AncestorsAndSelf().OfType<TNode>() ?? Enumerable.Empty<TNode>();
        }
    }
}
