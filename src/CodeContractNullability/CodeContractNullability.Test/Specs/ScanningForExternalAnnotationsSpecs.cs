using CodeContractNullability.ExternalAnnotations;
using TestableFileSystem.Fakes.Builders;
using TestableFileSystem.Interfaces;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    public sealed class ScanningForExternalAnnotationsSpecs
    {
        [Fact(Skip = "TODO: Implement tests")]
        public void XXX()
        {
            IFileSystem fileSystem = new FakeFileSystemBuilder().Build();

            var resolver = new CachingExternalAnnotationsResolver(fileSystem, new LocalAnnotationCacheProvider(fileSystem));

            resolver.EnsureScanned();
        }
    }
}
