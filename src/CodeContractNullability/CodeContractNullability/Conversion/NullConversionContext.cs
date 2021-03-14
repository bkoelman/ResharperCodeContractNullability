using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace CodeContractNullability.Conversion
{
    internal sealed class NullConversionContext
    {
        [NotNull]
        private readonly SyntaxNode primaryDeclarationSyntax;

        [NotNull]
        private readonly DocumentEditor primaryEditor;

        [NotNull]
        private readonly Dictionary<DocumentId, DocumentEditor> otherEditors = new();

        [NotNull]
        public FrameworkTypeCache TypeCache { get; }

        public CancellationToken CancellationToken { get; }

        private NullConversionContext([NotNull] SyntaxNode declarationSyntax, [NotNull] DocumentEditor editor, [NotNull] FrameworkTypeCache typeCache,
            CancellationToken cancellationToken)
        {
            primaryDeclarationSyntax = declarationSyntax;
            primaryEditor = editor;
            TypeCache = typeCache;
            CancellationToken = cancellationToken;
        }

        [ItemNotNull]
        public static async Task<NullConversionContext> Create([NotNull] SyntaxNode declarationSyntax, [NotNull] Document document,
            [NotNull] FrameworkTypeCache typeCache, CancellationToken cancellationToken)
        {
            Guard.NotNull(declarationSyntax, nameof(declarationSyntax));
            Guard.NotNull(document, nameof(document));
            Guard.NotNull(typeCache, nameof(typeCache));

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            return new NullConversionContext(declarationSyntax, editor, typeCache, cancellationToken);
        }

        [ItemNotNull]
        public async Task<DocumentEditor> GetEditorForDeclaration([NotNull] ISymbol declarationSymbol)
        {
            Guard.NotNull(declarationSymbol, nameof(declarationSymbol));

            SyntaxNode firstDeclarationSyntax = declarationSymbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(CancellationToken)).First();

            if (firstDeclarationSyntax.SyntaxTree.FilePath == primaryDeclarationSyntax.SyntaxTree.FilePath)
            {
                return primaryEditor;
            }

            Solution solution = primaryEditor.OriginalDocument.Project.Solution;
            Document otherDocument = solution.GetDocument(firstDeclarationSyntax.SyntaxTree);

            if (!otherEditors.ContainsKey(otherDocument.Id))
            {
                otherEditors[otherDocument.Id] = await DocumentEditor.CreateAsync(otherDocument, CancellationToken).ConfigureAwait(false);
            }

            return otherEditors[otherDocument.Id];
        }

        [ItemNotNull]
        public async Task<Solution> GetSolution()
        {
            Document primaryDocument = await GetDocumentFormatted(primaryEditor, CancellationToken).ConfigureAwait(false);
            Solution solution = primaryDocument.Project.Solution;

            if (otherEditors.Any())
            {
                foreach (Task<Document> documentTask in otherEditors.Values.Select(async e =>
                    await GetDocumentFormatted(e, CancellationToken).ConfigureAwait(false)))
                {
                    Document otherDocument = await documentTask.ConfigureAwait(false);
                    SourceText text = await otherDocument.GetTextAsync(CancellationToken).ConfigureAwait(false);
                    solution = solution.WithDocumentText(otherDocument.Id, text);
                }
            }

            return solution;
        }

        [ItemNotNull]
        private static async Task<Document> GetDocumentFormatted([NotNull] DocumentEditor editor, CancellationToken cancellationToken)
        {
            Document document = editor.GetChangedDocument();
            OptionSet options = document.Project.Solution.Workspace.Options;

            Document formatted = await Formatter.FormatAsync(document, Formatter.Annotation, options, cancellationToken).ConfigureAwait(false);

            return formatted;
        }
    }
}
