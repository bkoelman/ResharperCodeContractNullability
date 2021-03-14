using System;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeContractNullability.Conversion
{
    internal static class TypeDeclarationWriter
    {
        // From: internal const string Microsoft.CodeAnalysis.SyntaxNodeExtensions.IdAnnotationKind in Microsoft.CodeAnalysis.dll
        private const string SyntaxNodeExtensionsIdAnnotationKind = "Id";

        [NotNull]
        public static SyntaxNode ToNullableTypeSyntax([NotNull] SyntaxNode declarationTypeSyntax, [NotNull] ResharperNullabilitySymbolState nullabilityState)
        {
            Guard.NotNull(declarationTypeSyntax, nameof(declarationTypeSyntax));
            Guard.NotNull(nullabilityState, nameof(nullabilityState));

            string currentTypeName = declarationTypeSyntax.ToString();
            string newTypeName = currentTypeName;

            if (nullabilityState.PrimaryStatus == ResharperNullableStatus.CanBeNull)
            {
                newTypeName = AddQuestionMarkToTypeName(newTypeName);
            }

            if (nullabilityState.ItemStatus == ResharperNullableStatus.CanBeNull)
            {
                newTypeName = AddQuestionMarkToItemTypeName(newTypeName);
            }

            if (newTypeName != currentTypeName)
            {
                return SyntaxFactory.ParseTypeName(newTypeName).WithTriviaFrom(declarationTypeSyntax)
                    .WithAdditionalAnnotations(declarationTypeSyntax.GetAnnotations(SyntaxNodeExtensionsIdAnnotationKind))
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }

            return declarationTypeSyntax;
        }

        [NotNull]
        private static string AddQuestionMarkToTypeName([NotNull] string typeName)
        {
            return typeName.EndsWith("?", StringComparison.Ordinal) ? typeName : typeName + "?";
        }

        [NotNull]
        private static string AddQuestionMarkToItemTypeName([NotNull] string typeName)
        {
            int closingAngleIndex = typeName.LastIndexOf('>');

            if (closingAngleIndex != -1)
            {
                string leftPart = typeName.Substring(0, closingAngleIndex);
                string rightPart = typeName.Substring(closingAngleIndex);

                return leftPart.EndsWith("?", StringComparison.Ordinal) ? typeName : leftPart + "?" + rightPart;
            }

            int closingBracketIndex = typeName.LastIndexOf("[]", StringComparison.Ordinal);

            if (closingBracketIndex != -1)
            {
                string leftPart = typeName.Substring(0, closingBracketIndex);
                string rightPart = typeName.Substring(closingBracketIndex);

                return leftPart.EndsWith("?", StringComparison.Ordinal) ? typeName : leftPart + "?" + rightPart;
            }

            // Item*-attribute on type that is not an array/collection/task/lazy. Attribute has no meaning, so ignore it.
            return typeName;
        }
    }
}
