using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using CodeContractNullability.Conversion;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeContractNullability
{
    /// <summary>
    /// Provides a code fix to convert Resharper nullability annotations to C# syntax.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableReferenceTypeConversionCodeFixProvider))]
    [Shared]
    public sealed class NullableReferenceTypeConversionCodeFixProvider : CodeFixProvider
    {
        [ItemNotNull]
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(NullableReferenceTypeConversionAnalyzer.DiagnosticId);

        [NotNull]
        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        [NotNull]
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SemanticModel model = await context.Document.GetSemanticModelAsync().ConfigureAwait(false);
            var typeCache = new FrameworkTypeCache(model.Compilation);

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                SyntaxNode syntaxRoot =
                    await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                SyntaxNode targetSyntax = syntaxRoot.FindNode(context.Span);

                SyntaxNode declarationSyntax = TranslateField(targetSyntax);
                ISymbol declarationSymbol = DeclarationSyntaxToSymbol(declarationSyntax, model);

                RegisterFixForSyntaxNode(declarationSyntax, declarationSymbol, diagnostic, context, typeCache);
            }
        }

        [NotNull]
        private static SyntaxNode TranslateField([NotNull] SyntaxNode syntax)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return syntax is VariableDeclaratorSyntax fieldVariableSyntax
                ? fieldVariableSyntax.GetAncestorOrThis<FieldDeclarationSyntax>()
                : syntax;
        }

        [NotNull]
        private static ISymbol DeclarationSyntaxToSymbol([NotNull] SyntaxNode declarationSyntax, [NotNull] SemanticModel model)
        {
            if (declarationSyntax is FieldDeclarationSyntax fieldSyntax)
            {
                declarationSyntax = fieldSyntax.Declaration.Variables.First();
            }

            return model.GetDeclaredSymbol(declarationSyntax);
        }

        private void RegisterFixForSyntaxNode([NotNull] SyntaxNode declarationSyntax, [NotNull] ISymbol declarationSymbol,
            [NotNull] Diagnostic diagnostic, CodeFixContext context, [NotNull] FrameworkTypeCache typeCache)
        {
            var codeAction = CodeAction.Create("Convert to C# syntax",
                token => ChangeSolutionAsync(declarationSyntax, declarationSymbol, context.Document, typeCache, token),
                nameof(NullableReferenceTypeConversionCodeFixProvider));

            context.RegisterCodeFix(codeAction, diagnostic);
        }

        [ItemNotNull]
        private async Task<Solution> ChangeSolutionAsync([NotNull] SyntaxNode declarationSyntax,
            [NotNull] ISymbol declarationSymbol, [NotNull] Document document, [NotNull] FrameworkTypeCache typeCache,
            CancellationToken cancellationToken)
        {
            NullConversionContext context = await NullConversionContext
                .Create(declarationSyntax, document, typeCache, cancellationToken).ConfigureAwait(false);

            var scope = new NullConversionScope(context, declarationSyntax, declarationSymbol);

            await scope.RewriteDeclaration(ResharperNullabilitySymbolState.Default).ConfigureAwait(false);

            return await context.GetSolution().ConfigureAwait(false);
        }
    }
}
