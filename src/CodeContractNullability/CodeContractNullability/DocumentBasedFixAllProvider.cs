using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability
{
    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider" /> that fixes documents independently.
    /// <remarks>
    /// Based on sources from project https://github.com/DotNetAnalyzers/StyleCopAnalyzers.
    /// </remarks>
    /// </summary>
    internal abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        [NotNull]
        [ItemNotNull]
        public override Task<CodeAction> GetFixAsync([NotNull] FixAllContext fixAllContext)
        {
            CodeAction fixAction = null;

            string codeActionTitle = GetCodeActionTitle(fixAllContext.CodeActionEquivalenceKey);

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                {
                    fixAction = CodeAction.Create(codeActionTitle,
                        cancellationToken => GetDocumentFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                        fixAllContext.CodeActionEquivalenceKey);
                    break;
                }
                case FixAllScope.Project:
                {
                    fixAction = CodeAction.Create(codeActionTitle,
                        cancellationToken => GetProjectFixesAsync(fixAllContext.WithCancellationToken(cancellationToken),
                            fixAllContext.Project), fixAllContext.CodeActionEquivalenceKey);
                    break;
                }
                case FixAllScope.Solution:
                {
                    fixAction = CodeAction.Create(codeActionTitle,
                        cancellationToken => GetSolutionFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                        fixAllContext.CodeActionEquivalenceKey);
                    break;
                }
            }

            return Task.FromResult(fixAction);
        }

        [NotNull]
        protected abstract string GetCodeActionTitle([NotNull] string codeActionEquivalenceKey);

        /// <summary>
        /// Fixes all occurrences of a diagnostic in a specific document.
        /// </summary>
        /// <param name="fixAllContext">
        /// The context for the Fix All operation.
        /// </param>
        /// <param name="document">
        /// The document to fix.
        /// </param>
        /// <param name="diagnostics">
        /// The diagnostics to fix in the document.
        /// </param>
        /// <returns>
        /// <para>
        /// The new <see cref="SyntaxNode" /> representing the root of the fixed document.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        /// <see langword="null" />, if no changes were made to the document.
        /// </para>
        /// </returns>
        [NotNull]
        [ItemCanBeNull]
        protected abstract Task<SyntaxNode> FixAllInDocumentAsync([NotNull] FixAllContext fixAllContext,
            [NotNull] Document document, [ItemNotNull] ImmutableArray<Diagnostic> diagnostics);

        [ItemNotNull]
        private async Task<Document> GetDocumentFixesAsync([NotNull] FixAllContext fixAllContext)
        {
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentDiagnosticsToFix =
                await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

            if (!documentDiagnosticsToFix.TryGetValue(fixAllContext.Document, out ImmutableArray<Diagnostic> diagnostics))
            {
                return fixAllContext.Document;
            }

            SyntaxNode newRoot = await FixAllInDocumentAsync(fixAllContext, fixAllContext.Document, diagnostics)
                .ConfigureAwait(false);

            return newRoot == null ? fixAllContext.Document : fixAllContext.Document.WithSyntaxRoot(newRoot);
        }

        [ItemNotNull]
        private async Task<Solution> GetSolutionFixesAsync([NotNull] FixAllContext fixAllContext,
            [ItemNotNull] ImmutableArray<Document> documents)
        {
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentDiagnosticsToFix =
                await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

            Solution solution = fixAllContext.Solution;
            var newDocuments = new List<Task<SyntaxNode>>(documents.Length);

            foreach (Document document in documents)
            {
                if (!documentDiagnosticsToFix.TryGetValue(document, out ImmutableArray<Diagnostic> diagnostics))
                {
                    newDocuments.Add(document.GetSyntaxRootAsync(fixAllContext.CancellationToken));
                    continue;
                }

                newDocuments.Add(FixAllInDocumentAsync(fixAllContext, document, diagnostics));
            }

            for (int i = 0; i < documents.Length; i++)
            {
                SyntaxNode newDocumentRoot = await newDocuments[i].ConfigureAwait(false);
                if (newDocumentRoot == null)
                {
                    continue;
                }

                solution = solution.WithDocumentSyntaxRoot(documents[i].Id, newDocumentRoot);
            }

            return solution;
        }

        [NotNull]
        [ItemNotNull]
        private Task<Solution> GetProjectFixesAsync([NotNull] FixAllContext fixAllContext, [NotNull] Project project)
        {
            return GetSolutionFixesAsync(fixAllContext, project.Documents.ToImmutableArray());
        }

        [NotNull]
        [ItemNotNull]
        private Task<Solution> GetSolutionFixesAsync([NotNull] FixAllContext fixAllContext)
        {
            ImmutableArray<Document> documents = fixAllContext.Solution.Projects.SelectMany(i => i.Documents).ToImmutableArray();
            return GetSolutionFixesAsync(fixAllContext, documents);
        }
    }
}
