using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability
{
    /// <remarks>
    /// Based on sources from project https://github.com/DotNetAnalyzers/StyleCopAnalyzers.
    /// </remarks>
    internal static class FixAllContextHelper
    {
        [ItemNotNull]
        public static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            [NotNull] FixAllContext fixAllContext)
        {
            var allDiagnostics = ImmutableArray<Diagnostic>.Empty;
            var projectsToFix = ImmutableArray<Project>.Empty;

            Document document = fixAllContext.Document;
            Project project = fixAllContext.Project;

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                {
                    if (document != null)
                    {
                        ImmutableArray<Diagnostic> documentDiagnostics =
                            await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);

                        return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty.SetItem(document,
                            documentDiagnostics);
                    }

                    break;
                }
                case FixAllScope.Project:
                {
                    projectsToFix = ImmutableArray.Create(project);
                    allDiagnostics = await GetAllDiagnosticsAsync(fixAllContext, project).ConfigureAwait(false);

                    break;
                }
                case FixAllScope.Solution:
                {
                    projectsToFix = project.Solution.Projects.Where(p => p.Language == project.Language).ToImmutableArray();

                    var diagnostics = new ConcurrentDictionary<ProjectId, ImmutableArray<Diagnostic>>();
                    var tasks = new Task[projectsToFix.Length];

                    for (int i = 0; i < projectsToFix.Length; i++)
                    {
                        fixAllContext.CancellationToken.ThrowIfCancellationRequested();

                        Project projectToFix = projectsToFix[i];
                        tasks[i] = Task.Run(async () =>
                        {
                            ImmutableArray<Diagnostic> projectDiagnostics =
                                await GetAllDiagnosticsAsync(fixAllContext, projectToFix).ConfigureAwait(false);

                            diagnostics.TryAdd(projectToFix.Id, projectDiagnostics);
                        }, fixAllContext.CancellationToken);
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    allDiagnostics = allDiagnostics.AddRange(diagnostics.SelectMany(i =>
                        i.Value.Where(x => fixAllContext.DiagnosticIds.Contains(x.Id))));
                    break;
                }
            }

            if (allDiagnostics.IsEmpty)
            {
                return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
            }

            return await GetDocumentDiagnosticsToFixAsync(allDiagnostics, projectsToFix, fixAllContext.CancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all <see cref="Diagnostic" /> instances within a specific <see cref="Project" /> which are relevant to a
        /// <see cref="FixAllContext" />.
        /// </summary>
        /// <param name="fixAllContext">
        /// The context for the Fix All operation.
        /// </param>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}" /> representing the asynchronous operation. When the task completes successfully, the
        /// <see cref="Task{TResult}.Result" /> will contain the requested diagnostics.
        /// </returns>
        private static async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync([NotNull] FixAllContext fixAllContext,
            [NotNull] Project project)
        {
            return await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
        }

        [ItemNotNull]
        private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            [ItemNotNull] ImmutableArray<Diagnostic> diagnostics, [ItemNotNull] ImmutableArray<Project> projects,
            CancellationToken cancellationToken)
        {
            ImmutableDictionary<SyntaxTree, Document> treeToDocumentMap =
                await GetTreeToDocumentMapAsync(projects, cancellationToken).ConfigureAwait(false);

            ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Builder builder =
                ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();

            foreach (IGrouping<Document, Diagnostic> documentAndDiagnostics in diagnostics.GroupBy(d =>
                GetReportedDocument(d, treeToDocumentMap)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                Document document = documentAndDiagnostics.Key;
                ImmutableArray<Diagnostic> diagnosticsForDocument = documentAndDiagnostics.ToImmutableArray();

                builder.Add(document, diagnosticsForDocument);
            }

            return builder.ToImmutable();
        }

        [ItemNotNull]
        private static async Task<ImmutableDictionary<SyntaxTree, Document>> GetTreeToDocumentMapAsync(
            [ItemNotNull] ImmutableArray<Project> projects, CancellationToken cancellationToken)
        {
            ImmutableDictionary<SyntaxTree, Document>.Builder builder = ImmutableDictionary.CreateBuilder<SyntaxTree, Document>();
            foreach (Project project in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (Document document in project.Documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SyntaxTree tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    builder.Add(tree, document);
                }
            }

            return builder.ToImmutable();
        }

        [CanBeNull]
        private static Document GetReportedDocument([NotNull] Diagnostic diagnostic,
            [NotNull] ImmutableDictionary<SyntaxTree, Document> treeToDocumentsMap)
        {
            SyntaxTree tree = diagnostic.Location.SourceTree;
            return tree != null && treeToDocumentsMap.TryGetValue(tree, out Document document) ? document : null;
        }
    }
}
