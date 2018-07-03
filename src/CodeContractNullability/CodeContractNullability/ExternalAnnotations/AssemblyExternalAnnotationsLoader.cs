using System.IO;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.ExternalAnnotations
{
    /// <summary>
    /// Attempts to find and parse a side-by-side [AssemblyName].ExternalAnnotations.xml file that resides in the same folder as the
    /// assembly that contains the requested symbol.
    /// </summary>
    internal static class AssemblyExternalAnnotationsLoader
    {
        [CanBeNull]
        public static string GetPathForExternalSymbolOrNull([NotNull] ISymbol symbol, [NotNull] Compilation compilation)
        {
            Guard.NotNull(symbol, nameof(symbol));
            Guard.NotNull(compilation, nameof(compilation));

            if (symbol.ContainingAssembly != null)
            {
                var assemblyReference =
                    compilation.GetMetadataReference(symbol.ContainingAssembly) as PortableExecutableReference;

                string assemblyPath = assemblyReference?.FilePath;
                string folder = Path.GetDirectoryName(assemblyPath);
                if (folder != null)
                {
                    string assemblyFileName = Path.GetFileNameWithoutExtension(assemblyPath);
                    string annotationFilePath = Path.Combine(folder, assemblyFileName + ".ExternalAnnotations.xml");

                    return File.Exists(annotationFilePath) ? annotationFilePath : null;
                }
            }

            return null;
        }

        [NotNull]
        public static ExternalAnnotationsMap ParseFile([NotNull] string externalAnnotationsPath)
        {
            Guard.NotNull(externalAnnotationsPath, nameof(externalAnnotationsPath));

            using (TextReader reader = File.OpenText(externalAnnotationsPath))
            {
                var map = new ExternalAnnotationsMap();

                var parser = new ExternalAnnotationDocumentParser();
                parser.ProcessDocument(reader, map);

                return map;
            }
        }
    }
}
