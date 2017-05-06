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
    /// Provides a code fix that adds a nullability configuration file to the project.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateConfigurationCodeFixProvider))]
    [Shared]
    public sealed class CreateConfigurationCodeFixProvider : CodeFixProvider
    {
        [ItemNotNull]
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(BaseAnalyzer
            .CreateConfigurationDiagnosticId);

        [NotNull]
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                CodeAction codeAction = CodeAction.Create("Add nullability configuration file to project",
                    cancellationToken => AddConfigurationFileToProject(context));
                context.RegisterCodeFix(codeAction, diagnostic);
            }

            return Task.FromResult(0);
        }

        [NotNull]
        [ItemNotNull]
        private Task<Solution> AddConfigurationFileToProject(CodeFixContext context)
        {
            TextDocument existingDocument =
                context.Document.Project.AdditionalDocuments.FirstOrDefault(
                    document => SettingsProvider.IsSettingsFile(document.FilePath));

            if (existingDocument != null)
            {
                return Task.FromResult(context.Document.Project.Solution);
            }

            Project project = context.Document.Project;
            string content = SettingsProvider.ToFileContent(AnalyzerSettings.Default);

            TextDocument newDocument = project.AddAdditionalDocument(SettingsProvider.SettingsFileName, content);
            return Task.FromResult(newDocument.Project.Solution);
        }
    }
}
