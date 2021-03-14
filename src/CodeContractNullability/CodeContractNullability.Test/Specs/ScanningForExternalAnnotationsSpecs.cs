using System;
using System.IO;
using System.Text;
using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.Test.TestDataBuilders;
using FluentAssertions;
using FluentAssertions.Extensions;
using JetBrains.Annotations;
using TestableFileSystem.Fakes;
using TestableFileSystem.Fakes.Builders;
using TestableFileSystem.Interfaces;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    public sealed class ScanningForExternalAnnotationsSpecs
    {
        [Fact]
        public void When_external_annotations_are_not_found_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            // Act
            Action action = () => resolver.EnsureScanned();

            // Assert
            action.Should().ThrowExactly<MissingExternalAnnotationsException>();
        }

        [Fact]
        public void When_multiple_external_annotation_files_are_found_it_must_succeed()
        {
            // Arrange
            string externalAnnotationsDirectory = Environment.ExpandEnvironmentVariables(
                @"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs15\Extensions\.NETFramework\mscorlib");

            string annotationsFile1 = Path.Combine(externalAnnotationsDirectory, "Annotations1.xml");

            var clock = new SystemClock(() => 1.January(2001).AsUtc());

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingTextFile(annotationsFile1, new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:System.String.IsNullOrWhiteSpace(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("value")
                            .CanBeNull()))
                    .GetXml())
                .IncludingTextFile(Path.Combine(externalAnnotationsDirectory, "Annotations2.xml"), new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:System.String.IsNullOrEmpty(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("value")
                            .CanBeNull()))
                    .GetXml())
                .Build();

            fileSystem.File.SetLastWriteTimeUtc(annotationsFile1, 2.January(2001).AsUtc());

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            // Act
            Action action = () => resolver.EnsureScanned();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void When_external_annotations_contains_member_definition_without_nullability_it_must_succeed()
        {
            // Arrange
            string externalAnnotationsDirectory = Environment.ExpandEnvironmentVariables(
                @"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs15\Extensions\.NETFramework\mscorlib");

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingTextFile(Path.Combine(externalAnnotationsDirectory, "Annotations1.xml"), new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:System.String.IsNullOrWhiteSpace(System.String)"))
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:System.String.IsNullOrEmpty(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("value")
                            .CanBeNull()))
                    .GetXml())
                .Build();

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            // Act
            Action action = () => resolver.EnsureScanned();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void When_external_annotations_are_cached_it_must_succeed()
        {
            // Arrange
            string cachePath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\ResharperCodeContractNullability\external-annotations.cache");

            string externalAnnotationsPath = Environment.ExpandEnvironmentVariables(
                @"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs15\Extensions\.NETFramework\mscorlib\Annotations.xml");

            var clock = new SystemClock(() => 1.January(2001).AsUtc());

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingBinaryFile(cachePath,
                    Convert.FromBase64String("koHZKlN5c3RlbS5TdHJpbmcuSXNOdWxsT3JFbXB0eShTeXN0ZW0uU3RyaW5nKZPCgaV2YWx1ZcOhTdb/Ok/IgA=="))
                .IncludingTextFile(externalAnnotationsPath, new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:System.String.IsNullOrEmpty(System.String)"))
                    .GetXml())
                .Build();

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            // Act
            Action action = () => resolver.EnsureScanned();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void When_external_annotations_are_cached_without_external_annotations_on_disk_it_must_fail()
        {
            // Arrange
            string cachePath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\ResharperCodeContractNullability\external-annotations.cache");

            var clock = new SystemClock(() => 1.January(2001).AsUtc());

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingBinaryFile(cachePath,
                    Convert.FromBase64String("koHZKlN5c3RlbS5TdHJpbmcuSXNOdWxsT3JFbXB0eShTeXN0ZW0uU3RyaW5nKZPCgaV2YWx1ZcOhTdb/Ok/IgA=="))
                .Build();

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            // Act
            Action action = () => resolver.EnsureScanned();

            // Assert
            action.Should().ThrowExactly<MissingExternalAnnotationsException>();
        }

        [Fact]
        public void When_external_annotations_cache_is_corrupt_it_must_succeed()
        {
            // Arrange
            string cachePath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\ResharperCodeContractNullability\external-annotations.cache");

            string externalAnnotationsPath = Environment.ExpandEnvironmentVariables(
                @"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs15\Extensions\.NETFramework\mscorlib\Annotations.xml");

            var clock = new SystemClock(() => 1.January(2001).AsUtc());

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingBinaryFile(cachePath, Encoding.ASCII.GetBytes("*** BAD CACHE DATA ***"))
                .IncludingTextFile(externalAnnotationsPath, new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:System.String.IsNullOrEmpty(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("value")
                            .CanBeNull()))
                    .GetXml())
                .Build();

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            // Act
            Action action = () => resolver.EnsureScanned();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void When_side_by_side_external_annotations_file_does_not_exist_it_must_succeed()
        {
            // Arrange
            using (var assemblyScope = new TempAssemblyScope())
            {
                string systemAnnotationsPath = Environment.ExpandEnvironmentVariables(
                    @"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs14_57882815\ExternalAnnotations\.NETFramework\mscorlib\Annotations.xml");

                IFileSystem fileSystem = new FakeFileSystemBuilder()
                    .IncludingTextFile(systemAnnotationsPath, new ExternalAnnotationsBuilder()
                        .IncludingMember(new ExternalAnnotationFragmentBuilder()
                            .Named("M:System.String.IsNullOrEmpty(System.String)")
                            .WithParameter(new ExternalAnnotationParameterBuilder()
                                .Named("value")
                                .CanBeNull()))
                        .GetXml())
                    .Build();

                ParsedSourceCode source = new TypeSourceCodeBuilder()
                    .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                    .WithReferenceToExternalAssemblyOnDiskFor(assemblyScope.AssemblyPath, @"
                        using System;

                        namespace ExternalAssembly
                        {
                            public interface I
                            {
                                string M();
                            }
                        }
                    ")
                    .InGlobalScope(@"
                        public class C : ExternalAssembly.I
                        {
                            public string [|M|]()
                            {
                                throw null;
                            }
                        }
                    ")
                    .Build();

                var analyzerTest = new ReusableAnalyzerOnFileSystemTest(fileSystem);

                analyzerTest.VerifyNullabilityDiagnostics(source,
                    analyzerTest.CreateMessageFor(SymbolType.Method, "M"));
            }
        }

        [Fact]
        public void When_side_by_side_external_annotations_file_changes_it_must_update_analyzer_cache()
        {
            // Arrange
            using (var assemblyScope = new TempAssemblyScope())
            {
                string systemAnnotationsPath = Environment.ExpandEnvironmentVariables(
                    @"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs14_57882815\ExternalAnnotations\.NETFramework\mscorlib\Annotations.xml");

                string sideBySideAnnotationsPath = assemblyScope.TempPath + ".ExternalAnnotations.xml";

                IFileSystem fileSystem = new FakeFileSystemBuilder()
                    .IncludingTextFile(systemAnnotationsPath, new ExternalAnnotationsBuilder()
                        .IncludingMember(new ExternalAnnotationFragmentBuilder()
                            .Named("M:System.String.IsNullOrEmpty(System.String)")
                            .WithParameter(new ExternalAnnotationParameterBuilder()
                                .Named("value")
                                .CanBeNull()))
                        .GetXml())
                    .IncludingTextFile(sideBySideAnnotationsPath, new ExternalAnnotationsBuilder()
                        .IncludingMember(new ExternalAnnotationFragmentBuilder()
                            .Named("M:ExternalAssembly.I.M")
                            .NotNull())
                        .GetXml())
                    .Build();

                TypeSourceCodeBuilder sourceBuilder = new TypeSourceCodeBuilder()
                    .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                    .WithReferenceToExternalAssemblyOnDiskFor(assemblyScope.AssemblyPath, @"
                        using System;

                        namespace ExternalAssembly
                        {
                            public interface I
                            {
                                string M();
                            }
                        }
                    ");

                ParsedSourceCode initialSource = sourceBuilder
                    .InGlobalScope(@"
                        public class C : ExternalAssembly.I
                        {
                            public string M()
                            {
                                throw null;
                            }
                        }
                    ")
                    .Build();

                // Method ExternalAssembly.I.M() is externally annotated in side-by-side xml file, so expect no diagnostics.
                var analyzerTest = new ReusableAnalyzerOnFileSystemTest(fileSystem);
                analyzerTest.VerifyNullabilityDiagnostics(initialSource);

                // Update contents of the side-by-side xml file: remove the external annotation.
                fileSystem.File.WriteAllText(sideBySideAnnotationsPath, new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:ExternalAssembly.I.M"))
                    .GetXml());

                // Update source text (add markers) without recreating analyzer instance.
                ParsedSourceCode updatedSource = sourceBuilder
                    .ClearGlobalScope()
                    .InGlobalScope(@"
                        public class C : ExternalAssembly.I
                        {
                            public string [|M|]()
                            {
                                throw null;
                            }
                        }
                    ")
                    .Build();

                analyzerTest.WaitForFileEvictionFromSideBySideCache(sideBySideAnnotationsPath);

                // After update of the side-by-side file on disk, analyzer should detect the change and report a diagnostic this time.
                analyzerTest.VerifyNullabilityDiagnostics(updatedSource,
                    analyzerTest.CreateMessageFor(SymbolType.Method, "M"));
            }
        }

        [Theory]
        [InlineData(@"%SystemDrive%\Program Files\JetBrains\JetBrains Rider 2018.2.1\lib\ReSharperHost\ExternalAnnotations\ExternalAnnotations")]
        [InlineData(@"%ProgramFiles(x86)%\JetBrains\Installations\ReSharperPlatformVs14\ExternalAnnotations")]
        [InlineData(@"%ProgramFiles(x86)%\JetBrains\Installations\ReSharperPlatformVs14\Extensions")]
        [InlineData(@"%USERPROFILE%\.nuget\packages\jetbrains.externalannotations\10.2.57\DotFiles\ExternalAnnotations")]
        [InlineData(@"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs14_57882815\ExternalAnnotations")]
        [InlineData(@"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs14_57882815\Extensions")]
        [InlineData(@"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs15\ExternalAnnotations")]
        [InlineData(@"%LOCALAPPDATA%\JetBrains\Installations\ReSharperPlatformVs15\Extensions")]
        public void When_external_annotations_are_found_it_must_succeed([NotNull] string externalAnnotationsDirectory)
        {
            // Arrange
            string externalAnnotationsPath = Path.Combine(Environment.ExpandEnvironmentVariables(externalAnnotationsDirectory),
                @".NETFramework\mscorlib\Annotations.xml");

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingTextFile(externalAnnotationsPath, new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:System.String.IsNullOrEmpty(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("value")
                            .CanBeNull()))
                    .GetXml())
                .Build();

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            // Act
            Action action = () => resolver.EnsureScanned();

            // Assert
            action.Should().NotThrow();
        }
    }
}
