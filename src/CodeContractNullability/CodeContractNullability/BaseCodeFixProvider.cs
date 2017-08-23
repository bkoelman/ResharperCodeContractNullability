using System;
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
            return WellKnownFixAllProviders.BatchFixer;
        }

        [NotNull]
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                NullabilityAttributeSymbols nullSymbols =
                    await GetNullabilityAttributesFromDiagnostic(context, diagnostic).ConfigureAwait(false);

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
        private static async Task<NullabilityAttributeSymbols> GetNullabilityAttributesFromDiagnostic(CodeFixContext context,
            [NotNull] Diagnostic diagnostic)
        {
            NullabilityAttributeMetadataNames names =
                NullabilityAttributeMetadataNames.FromImmutableDictionary(diagnostic.Properties);

            Compilation compilation =
                await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);

            var attributeProvider = new CachingNullabilityAttributeProvider(names);
            NullabilityAttributeSymbols nullSymbols = attributeProvider.GetSymbols(compilation, context.CancellationToken);
            if (nullSymbols == null)
            {
                throw new InvalidOperationException("Internal error: failed to resolve attributes.");
            }
            return nullSymbols;
        }

        private void RegisterFixesForSyntaxNode(CodeFixContext context, [NotNull] SyntaxNode syntaxNode,
            [NotNull] Diagnostic diagnostic, [NotNull] NullabilityAttributeSymbols nullSymbols)
        {
            RegisterFixForNotNull(context, syntaxNode, diagnostic, nullSymbols);
            RegisterFixForCanBeNull(context, syntaxNode, diagnostic, nullSymbols);
        }

        private void RegisterFixForNotNull(CodeFixContext context, [NotNull] SyntaxNode syntaxNode,
            [NotNull] Diagnostic diagnostic, [NotNull] NullabilityAttributeSymbols nullSymbols)
        {
            INamedTypeSymbol notNullAttribute = appliesToItem ? nullSymbols.ItemNotNull : nullSymbols.NotNull;

            Func<CancellationToken, Task<Document>> fixForNotNull = cancellationToken =>
                WithAttributeAsync(notNullAttribute, context.Document, syntaxNode, cancellationToken);

            string notNullText = "Decorate with " + GetDisplayNameFor(notNullAttribute);
            RegisterCodeFixFor(fixForNotNull, notNullText, context, diagnostic);
        }

        private void RegisterFixForCanBeNull(CodeFixContext context, [NotNull] SyntaxNode syntaxNode,
            [NotNull] Diagnostic diagnostic, [NotNull] NullabilityAttributeSymbols nullSymbols)
        {
            INamedTypeSymbol canBeNullAttribute = appliesToItem ? nullSymbols.ItemCanBeNull : nullSymbols.CanBeNull;

            Func<CancellationToken, Task<Document>> fixForCanBeNull = cancellationToken =>
                WithAttributeAsync(canBeNullAttribute, context.Document, syntaxNode, cancellationToken);

            string canBeNullText = "Decorate with " + GetDisplayNameFor(canBeNullAttribute);
            RegisterCodeFixFor(fixForCanBeNull, canBeNullText, context, diagnostic);
        }

        [NotNull]
        private static string GetDisplayNameFor([NotNull] INamedTypeSymbol attribute)
        {
            return attribute.Name.Replace("Attribute", "");
        }

        [NotNull]
        [ItemNotNull]
        private async Task<Document> WithAttributeAsync([NotNull] INamedTypeSymbol attribute, [NotNull] Document document,
            [NotNull] SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            OptionSet options = document.Project.Solution.Workspace.Options;

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Add NotNull/CanBeNull/ItemNotNull/ItemCanBeNull attribute.
            SyntaxNode attributeSyntax = editor.Generator.Attribute(editor.Generator.TypeExpression(attribute))
                .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation, NamespaceImportAnnotation);
            editor.AddAttribute(syntaxNode, attributeSyntax);
            Document documentWithAttribute = editor.GetChangedDocument();

            // Add namespace import.
            Document documentWithImport = await ImportAdder
                .AddImportsAsync(documentWithAttribute, NamespaceImportAnnotation, options, cancellationToken)
                .ConfigureAwait(false);

            // Simplify and reformat all annotated nodes.
            Document simplified =
                await Simplifier.ReduceAsync(documentWithImport, options, cancellationToken).ConfigureAwait(false);
            Document formatted = await Formatter.FormatAsync(simplified, options, cancellationToken).ConfigureAwait(false);
            return formatted;
        }

        private void RegisterCodeFixFor([NotNull] Func<CancellationToken, Task<Document>> applyFixAction,
            [NotNull] string description, CodeFixContext context, [NotNull] Diagnostic diagnostic)
        {
            CodeAction codeAction = CodeAction.Create(description, applyFixAction, description);
            context.RegisterCodeFix(codeAction, diagnostic);
        }
    }
}
