using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability
{
    /// <summary />
    public static class SyntaxNodeExtensions
    {
        [CanBeNull]
        public static TNode GetAncestorOrThis<TNode>([CanBeNull] this SyntaxNode node)
            where TNode : SyntaxNode
        {
            return GetAncestorsOrThis<TNode>(node).FirstOrDefault();
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<TNode> GetAncestorsOrThis<TNode>([CanBeNull] this SyntaxNode node)
            where TNode : SyntaxNode
        {
            return node?.AncestorsAndSelf().OfType<TNode>() ?? new TNode[0];
        }
    }
}
