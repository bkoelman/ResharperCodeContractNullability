using System;
using System.IO;
using CodeContractNullability.ExternalAnnotations;
using FluentAssertions;
using TestableFileSystem.Fakes.Builders;
using TestableFileSystem.Interfaces;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    public sealed class ScanningForExternalAnnotationsSpecs
    {
        [Fact]
        public void When_scanning_for_external_annotations_it_must_succeed()
        {
            // Arrange
            string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(localAppDataFolder,
                @"JetBrains\Installations\ReSharperPlatformVs15_57882815\ExternalAnnotations\.NETFramework\mscorlib\Annotations.xml");

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingTextFile(filePath,
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <assembly name=""mscorlib"">
                      <member name=""M:System.String.Format(System.String,System.Object)"">
                        <attribute ctor=""M:JetBrains.Annotations.NotNullAttribute.#ctor""/>
                        <parameter name=""format"">
                          <attribute ctor=""M:JetBrains.Annotations.NotNullAttribute.#ctor""/>
                        </parameter>
                      </member>
                    </assembly>
                ")
                .Build();

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            // Act
            Action action = () => resolver.EnsureScanned();

            // Assert
            action.Should().NotThrow();
        }
    }
}
