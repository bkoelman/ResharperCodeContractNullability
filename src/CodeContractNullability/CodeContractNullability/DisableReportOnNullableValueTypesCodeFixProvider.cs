using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using CodeContractNullability.Settings;
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
    public sealed class DisableReportOnNullableValueTypesCodeFixProvider : CodeFixProvider
    {
        [ItemNotNull]
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(BaseAnalyzer
            .DisableReportOnNullableValueTypesDiagnosticId);

        [NotNull]
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                CodeAction codeAction = CodeAction.Create("Disable reporting on nullable value types in project",
                    cancellationToken => CreateOrUpdateSolution(context));
                context.RegisterCodeFix(codeAction, diagnostic);
            }

            return Task.FromResult(0);
        }

        [NotNull]
        [ItemNotNull]
        private async Task<Solution> CreateOrUpdateSolution(CodeFixContext context)
        {
            TextDocument existingDocument =
                context.Document.Project.AdditionalDocuments.FirstOrDefault(
                    document => SettingsProvider.IsSettingsFile(document.FilePath));

            AnalyzerSettings settings = AnalyzerSettings.Default;

            Project project = context.Document.Project;
            if (existingDocument != null)
            {
                settings = await SettingsProvider.LoadSettings(existingDocument, context.CancellationToken);
                project = project.RemoveAdditionalDocument(existingDocument.Id);
            }

            string newContent = SettingsProvider.ToFileContent(settings.WithDisableReportOnNullableValueTypes(true));
            TextDocument newDocument = project.AddAdditionalDocument(SettingsProvider.SettingsFileName, newContent);
            return newDocument.Project.Solution;
        }
    }
}
