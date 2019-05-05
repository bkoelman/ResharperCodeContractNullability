using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeContractNullability.NullabilityAttributes;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace CodeContractNullability
{
    /// <summary>
    /// Common functionality for all the fix providers that are included in this project.
    /// </summary>
    public abstract class BaseCodeFixProvider : CodeFixProvider
    {
        [NotNull]
        private static readonly SyntaxAnnotation NamespaceImportAnnotation = new SyntaxAnnotation();

        private readonly bool appliesToItem;

        protected BaseCodeFixProvider(bool appliesToItem)
        {
            this.appliesToItem = appliesToItem;
        }

        [NotNull]
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // Note: this may annotate too much. For instance, when an interface is annotated, its implementation should not.
            // But because at the time of analysis, both are not annotated, a diagnostic is created for both.

            return NullabilityFixAllProvider.Instance;
        }

        [NotNull]
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                NullabilityAttributeSymbols nullSymbols =
                    await GetNullabilityAttributesFromDiagnostic(diagnostic, context.Document, context.CancellationToken)
                        .ConfigureAwait(false);

                SyntaxNode syntaxRoot =
                    await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                SyntaxNode targetSyntax = syntaxRoot.FindNode(context.Span);

                FieldDeclarationSyntax fieldSyntax = targetSyntax is VariableDeclaratorSyntax
                    ? targetSyntax.GetAncestorOrThis<FieldDeclarationSyntax>()
                    : null;

                if (targetSyntax is MethodDeclarationSyntax || targetSyntax is IndexerDeclarationSyntax ||
                    targetSyntax is PropertyDeclarationSyntax || targetSyntax is ParameterSyntax || fieldSyntax != null)
                {
                    RegisterFixesForSyntaxNode(context, fieldSyntax ?? targetSyntax, diagnostic, nullSymbols);
                }
            }
        }

        [NotNull]
        [ItemNotNull]
        private static async Task<NullabilityAttributeSymbols> GetNullabilityAttributesFromDiagnostic(
            [NotNull] Diagnostic diagnostic, [NotNull] Document document, CancellationToken cancellationToken)
        {
            NullabilityAttributeMetadataNames names =
                NullabilityAttributeMetadataNames.FromImmutableDictionary(diagnostic.Properties);

            Compilation compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var attributeProvider = new CachingNullabilityAttributeProvider(names);
            NullabilityAttributeSymbols nullSymbols = attributeProvider.GetSymbols(compilation, cancellationToken);
            if (nullSymbols == null)
            {
                throw new InvalidOperationException("Internal error: failed to resolve attributes.");
            }

            return nullSymbols;
        }

        private void RegisterFixesForSyntaxNode(CodeFixContext context, [NotNull] SyntaxNode syntaxNode,
            [NotNull] Diagnostic diagnostic, [NotNull] NullabilityAttributeSymbols nullSymbols)
        {
            INamedTypeSymbol nullabilityNotNullAttribute = GetNullabilityAttribute(nullSymbols, appliesToItem, false);
            RegisterFixForAttribute(context, syntaxNode, diagnostic, nullabilityNotNullAttribute);

            INamedTypeSymbol nullabilityCanBeNullAttribute = GetNullabilityAttribute(nullSymbols, appliesToItem, true);
            RegisterFixForAttribute(context, syntaxNode, diagnostic, nullabilityCanBeNullAttribute);
        }

        [NotNull]
        private static INamedTypeSymbol GetNullabilityAttribute([NotNull] NullabilityAttributeSymbols nullSymbols,
            bool appliesToItem, bool canBeNull)
        {
            return appliesToItem
                ? (canBeNull ? nullSymbols.ItemCanBeNull : nullSymbols.ItemNotNull)
                : (canBeNull ? nullSymbols.CanBeNull : nullSymbols.NotNull);
        }

        private void RegisterFixForAttribute(CodeFixContext context, [NotNull] SyntaxNode syntaxNode,
            [NotNull] Diagnostic diagnostic, [NotNull] INamedTypeSymbol nullabilityAttribute)
        {
            string description = "Decorate with " + nullabilityAttribute.Name.Replace("Attribute", "");

            context.RegisterCodeFix(
                CodeAction.Create(description,
                    cancellationToken => ApplyCodeFixAsync(syntaxNode, context.Document, nullabilityAttribute, cancellationToken),
                    description), diagnostic);
        }

        [NotNull]
        [ItemNotNull]
        private async Task<Document> ApplyCodeFixAsync([NotNull] SyntaxNode syntaxNode, [NotNull] Document document,
            [NotNull] INamedTypeSymbol attribute, CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            AddNullabilityAttributeToSyntaxNode(syntaxNode, editor, attribute);

            Document documentWithAttribute = editor.GetChangedDocument();

            return await ImportNamespacesAsync(documentWithAttribute, cancellationToken).ConfigureAwait(false);
        }

        private static void AddNullabilityAttributeToSyntaxNode([NotNull] SyntaxNode syntaxNode, [NotNull] DocumentEditor editor,
            [NotNull] INamedTypeSymbol attributeToAdd)
        {
            SyntaxNode attributeSyntax = editor.Generator.Attribute(editor.Generator.TypeExpression(attributeToAdd))
                .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation, NamespaceImportAnnotation);

            editor.AddAttribute(syntaxNode, attributeSyntax);
        }

        [ItemNotNull]
        private static async Task<Document> ImportNamespacesAsync([NotNull] Document document,
            CancellationToken cancellationToken)
        {
            OptionSet options = document.Project.Solution.Workspace.Options;

            return await ImportAdder.AddImportsAsync(document, NamespaceImportAnnotation, options, cancellationToken)
                .ConfigureAwait(false);
        }

        [NotNull]
        private static INamedTypeSymbol GetNullabilityAttributeForEquivalenceKey(
            [NotNull] NullabilityAttributeSymbols nullSymbols, [NotNull] string equivalenceKey)
        {
            switch (equivalenceKey)
            {
                case "Decorate with NotNull":
                {
                    return nullSymbols.NotNull;
                }
                case "Decorate with CanBeNull":
                {
                    return nullSymbols.CanBeNull;
                }
                case "Decorate with ItemNotNull":
                {
                    return nullSymbols.ItemNotNull;
                }
                case "Decorate with ItemCanBeNull":
                {
                    return nullSymbols.ItemCanBeNull;
                }
            }

            throw new NotSupportedException($"Unsupported equivalence key '{equivalenceKey}'.");
        }

        private sealed class NullabilityFixAllProvider : DocumentBasedFixAllProvider
        {
            [NotNull]
            public static FixAllProvider Instance { get; } = new NullabilityFixAllProvider();

            protected override string GetCodeActionTitle(string codeActionEquivalenceKey) => codeActionEquivalenceKey;

            protected override async Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document,
                ImmutableArray<Diagnostic> diagnostics)
            {
                if (diagnostics.IsEmpty)
                {
                    return null;
                }

                DocumentEditor editor = await DocumentEditor.CreateAsync(document, fixAllContext.CancellationToken)
                    .ConfigureAwait(false);

                NullabilityAttributeSymbols nullSymbols =
                    await GetNullabilityAttributesFromDiagnostic(diagnostics.First(), document, fixAllContext.CancellationToken)
                        .ConfigureAwait(false);

                SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

                foreach (Diagnostic diagnostic in diagnostics)
                {
                    fixAllContext.CancellationToken.ThrowIfCancellationRequested();

                    SyntaxNode targetSyntax = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, false, true);

                    INamedTypeSymbol attributeToAdd =
                        GetNullabilityAttributeForEquivalenceKey(nullSymbols, fixAllContext.CodeActionEquivalenceKey);
                    AddNullabilityAttributeToSyntaxNode(targetSyntax, editor, attributeToAdd);
                }

                Document documentChanged = editor.GetChangedDocument();

                Document documentFormatted = await ImportNamespacesAsync(documentChanged, fixAllContext.CancellationToken)
                    .ConfigureAwait(false);

                return await documentFormatted.GetSyntaxRootAsync().ConfigureAwait(false);
            }
        }
    }
}
