using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeContractNullability.Conversion
{
    internal static class ResharperNullabilityAttributesRemover
    {
        [ItemNotNull]
        public static async Task<ResharperNullabilitySymbolState> RemoveFromDeclaration([NotNull] ISymbol declarationSymbol,
            [NotNull] SyntaxNode declarationSyntax, [NotNull] ResharperNullabilitySymbolState baseState,
            [NotNull] DocumentEditor editor, CancellationToken cancellationToken)
        {
            Guard.NotNull(declarationSymbol, nameof(declarationSymbol));
            Guard.NotNull(declarationSyntax, nameof(declarationSyntax));
            Guard.NotNull(baseState, nameof(baseState));
            Guard.NotNull(editor, nameof(editor));

            var primaryStatus = ResharperNullableStatus.Unspecified;
            var itemStatus = ResharperNullableStatus.Unspecified;

            foreach (AttributeData attribute in declarationSymbol.GetAttributes())
            {
                AttributeSyntax attributeSyntax =
                    await GetAttributeSyntaxAsync(attribute, cancellationToken).ConfigureAwait(false);

                switch (attribute.AttributeClass.Name)
                {
                    case "NotNullAttribute":
                    {
                        primaryStatus = ResharperNullableStatus.NotNull;
                        break;
                    }

                    case "CanBeNullAttribute":
                    {
                        primaryStatus = ResharperNullableStatus.CanBeNull;
                        break;
                    }

                    case "ItemNotNullAttribute":
                    {
                        itemStatus = ResharperNullableStatus.NotNull;
                        break;
                    }

                    case "ItemCanBeNullAttribute":
                    {
                        itemStatus = ResharperNullableStatus.CanBeNull;
                        break;
                    }

                    default:
                    {
                        continue;
                    }
                }

                // On partial methods, the attribute is returned even if it was declared on the *other* part.
                // So make sure that what we found actually exists in *this* subtree.
                if (IsSyntaxParentOf(attributeSyntax, declarationSyntax))
                {
                    RemoveAttributeFromList(attributeSyntax, editor);
                }
            }

            var thisState = new ResharperNullabilitySymbolState(primaryStatus, itemStatus);
            return baseState.ApplyOverride(thisState);
        }

        [ItemNotNull]
        private static async Task<AttributeSyntax> GetAttributeSyntaxAsync([NotNull] AttributeData attributeData,
            CancellationToken cancellationToken)
        {
            SyntaxReference syntaxReference = attributeData.ApplicationSyntaxReference;
            SyntaxNode attributeSyntax = await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            return (AttributeSyntax)attributeSyntax;
        }

        private static bool IsSyntaxParentOf([NotNull] SyntaxNode syntaxNode, [NotNull] SyntaxNode parentNode)
        {
            SyntaxNode currentNode = syntaxNode;
            while (currentNode != null)
            {
                if (currentNode == parentNode)
                {
                    return true;
                }

                currentNode = currentNode.Parent;
            }

            return false;
        }

        private static void RemoveAttributeFromList([NotNull] AttributeSyntax attributeSyntax, [NotNull] DocumentEditor editor)
        {
            string attributeName = attributeSyntax.Name.ToString();

            editor.ReplaceNode(attributeSyntax.Parent, (parent, gen) =>
            {
                if (parent == null)
                {
                    throw new Exception("Internal error: Unexpected null return value (1).");
                }

                AttributeSyntax newAttributeSyntax = parent.DescendantNodes().OfType<AttributeSyntax>()
                    .First(s => s.Name.ToString() == attributeName);

                var newAttributeListSyntax = (AttributeListSyntax)newAttributeSyntax.Parent;

                return newAttributeListSyntax.Attributes.Count == 1
                    ? RemoveSurroundingWhitespaceOnLine(newAttributeListSyntax)
                    : parent;
            });

            editor.ReplaceNode(attributeSyntax.Parent.Parent, (root, gen) =>
            {
                AttributeSyntax newAttributeSyntax = root.DescendantNodes().OfType<AttributeSyntax>()
                    .First(s => s.Name.ToString() == attributeName);

                var newAttributeListSyntax = (AttributeListSyntax)newAttributeSyntax.Parent;

                if (newAttributeListSyntax.Attributes.Count == 1)
                {
                    SyntaxNode returnValue1 = root.RemoveNode(newAttributeListSyntax, SyntaxRemoveOptions.KeepExteriorTrivia)
                        .WithAdditionalAnnotations(Formatter.Annotation);

                    if (returnValue1 == null)
                    {
                        throw new Exception("Internal error: Unexpected null return value (2).");
                    }

                    return returnValue1;
                }

                AttributeListSyntax listWithoutAttributeSyntax = newAttributeListSyntax
                    .RemoveNode(newAttributeSyntax, SyntaxRemoveOptions.KeepExteriorTrivia)
                    .WithAdditionalAnnotations(Formatter.Annotation);

                SyntaxNode returnValue2 = root.ReplaceNode(newAttributeListSyntax, listWithoutAttributeSyntax);

                if (returnValue2 == null)
                {
                    throw new Exception("Internal error: Unexpected null return value (3).");
                }

                return returnValue2;
            });
        }

        [NotNull]
        private static T RemoveSurroundingWhitespaceOnLine<T>([NotNull] T syntax)
            where T : SyntaxNode
        {
            IList<SyntaxTrivia> trailingTrivia = TryRemoveTrailingWhitespaceIncludingEndOfLine(syntax);
            if (trailingTrivia != null)
            {
                IList<SyntaxTrivia> leadingTrivia = TryRemoveLeadingWhitespaceOnSameLine(syntax);
                if (leadingTrivia != null)
                {
                    return syntax.WithTrailingTrivia(trailingTrivia).WithLeadingTrivia(leadingTrivia);
                }
            }

            return syntax;
        }

        [CanBeNull]
        private static IList<SyntaxTrivia> TryRemoveTrailingWhitespaceIncludingEndOfLine([NotNull] SyntaxNode syntax)
        {
            List<SyntaxTrivia> trailingTrivia = syntax.GetTrailingTrivia().ToList();

            for (int index = 0; index < trailingTrivia.Count && !IsExplicitEndOfLine(trailingTrivia[index]); index++)
            {
                if (trailingTrivia[index].Kind() == SyntaxKind.WhitespaceTrivia)
                {
                    trailingTrivia.RemoveAt(index);
                    index--;
                }
                else
                {
                    // Non-whitespace found on same line, so this line cannot be removed.
                    return null;
                }
            }

            if (trailingTrivia.Count == 0 || !IsExplicitEndOfLine(trailingTrivia[0]))
            {
                // No end-of-line found, so this line cannot be removed.
                return null;
            }

            trailingTrivia.RemoveAt(0);
            return trailingTrivia;
        }

        [CanBeNull]
        private static IList<SyntaxTrivia> TryRemoveLeadingWhitespaceOnSameLine([NotNull] SyntaxNode syntax)
        {
            List<SyntaxTrivia> leadingTrivia = syntax.GetLeadingTrivia().ToList();

            for (int index = leadingTrivia.Count - 1; index >= 0 && !IsImplicitEndOfLine(leadingTrivia[index]); index--)
            {
                if (leadingTrivia[index].Kind() == SyntaxKind.WhitespaceTrivia)
                {
                    leadingTrivia.RemoveAt(index);
                }
                else
                {
                    // Non-whitespace found on same line, so this line cannot be removed.
                    return null;
                }
            }

            return leadingTrivia;
        }

        private static bool IsImplicitEndOfLine(SyntaxTrivia trivia)
        {
            return trivia.IsDirective || trivia.Kind() == SyntaxKind.EndOfLineTrivia ||
                trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia;
        }

        private static bool IsExplicitEndOfLine(SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.EndOfLineTrivia;
        }
    }
}
