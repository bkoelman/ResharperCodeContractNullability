using System;
using System.Threading;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using TestableFileSystem.Wrappers;

namespace CodeContractNullability.ExternalAnnotations
{
    public sealed class GlobalAnnotationCacheProvider : ICacheProvider<ExternalAnnotationsMap>
    {
        [NotNull]
        [ItemNotNull]
        private static readonly Lazy<ExternalAnnotationsMap> GlobalCache;

        static GlobalAnnotationCacheProvider()
        {
            GlobalCache = new Lazy<ExternalAnnotationsMap>(new FolderExternalAnnotationsLoader(FileSystemWrapper.Default).Create,
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public ExternalAnnotationsMap GetValue()
        {
            return GlobalCache.Value;
        }
    }
}
