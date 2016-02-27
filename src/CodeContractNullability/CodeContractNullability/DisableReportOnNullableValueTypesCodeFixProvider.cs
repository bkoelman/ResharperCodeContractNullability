using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability
{
    /// <summary>
    /// Provides a code fix to create a configuration file that disables reporting on nullable value types.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisableReportOnNullableValueTypesCodeFixProvider))]
    [Shared]
    public class DisableReportOnNullableValueTypesCodeFixProvider : CodeFixProvider
    {
        [ItemNotNull]
        public override sealed ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(BaseAnalyzer.DisableReportOnNullableValueTypesDiagnosticId);

        [NotNull]
        public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            await Task.Yield();

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                CodeAction codeAction = CodeAction.Create("Disable reporting on nullable value types in project",
                    cancellationToken => CreateOrUpdateSolution(context));
                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        [NotNull]
        [ItemNotNull]
        private Task<Solution> CreateOrUpdateSolution(CodeFixContext context)
        {
            string content = SettingsProvider.ToFileContent(new AnalyzerSettings(true));

            TextDocument existingDocument =
                context.Document.Project.AdditionalDocuments.FirstOrDefault(
                    document => SettingsProvider.IsSettingsFile(document.FilePath));

            Project project = context.Document.Project;
            if (existingDocument != null)
            {
                project = project.RemoveAdditionalDocument(existingDocument.Id);
            }

            TextDocument newDocument = project.AddAdditionalDocument(SettingsProvider.SettingsFileName, content);
            return Task.FromResult(newDocument.Project.Solution);
        }
    }
}