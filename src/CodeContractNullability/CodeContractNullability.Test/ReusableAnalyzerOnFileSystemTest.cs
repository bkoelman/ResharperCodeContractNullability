using System;
using System.Threading;
using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.NullabilityAttributes;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Diagnostics;
using TestableFileSystem.Interfaces;

namespace CodeContractNullability.Test
{
    internal sealed class ReusableAnalyzerOnFileSystemTest : NullabilityTest
    {
        [NotNull]
        private readonly IFileSystem fileSystem;

        [CanBeNull]
        private BaseAnalyzer analyzer;

        [CanBeNull]
        private CachingExternalAnnotationsResolver externalAnnotationsResolver;

        public ReusableAnalyzerOnFileSystemTest([NotNull] IFileSystem fileSystem)
        {
            Guard.NotNull(fileSystem, nameof(fileSystem));
            this.fileSystem = fileSystem;
        }

        public void VerifyNullabilityDiagnostics([NotNull] ParsedSourceCode source,
            [NotNull] [ItemNotNull] params string[] messages)
        {
            VerifyNullabilityDiagnostic(source, messages);
        }

        [NotNull]
        public string CreateMessageFor(SymbolType symbolType, [NotNull] string name)
        {
            switch (symbolType)
            {
                case SymbolType.Field:
                {
                    return CreateMessageForField(name);
                }
                case SymbolType.Property:
                {
                    return CreateMessageForProperty(name);
                }
                case SymbolType.Method:
                {
                    return CreateMessageForMethod(name);
                }
                case SymbolType.Parameter:
                {
                    return CreateMessageForParameter(name);
                }
                default:
                {
                    throw new NotSupportedException();
                }
            }
        }

        public void WaitForFileEvictionFromSideBySideCache([NotNull] string path)
        {
            Guard.NotNull(path, nameof(path));

            if (externalAnnotationsResolver != null)
            {
                TimeSpan timeout = TimeSpan.FromSeconds(3);
                DateTime startTime = DateTime.UtcNow;

                while (startTime + timeout > DateTime.UtcNow)
                {
                    if (!externalAnnotationsResolver.IsFileInSideBySideCache(path))
                    {
                        return;
                    }

                    Thread.Sleep(50);
                }
            }

            throw new TimeoutException($"Timed out waiting for eviction of '{path}' from side-by-side cache.");
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            if (analyzer == null)
            {
                analyzer = CreateNullabilityAnalyzer();

                externalAnnotationsResolver =
                    new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

                analyzer.FileSystem.Override(fileSystem);
                analyzer.ExternalAnnotationsResolver.Override(externalAnnotationsResolver);
                analyzer.NullabilityAttributeProvider.Override(new SimpleNullabilityAttributeProvider());
            }

            return analyzer;
        }
    }
}
