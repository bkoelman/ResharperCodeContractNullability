using System;
using System.Collections.Concurrent;
using System.IO;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using TestableFileSystem.Interfaces;

namespace CodeContractNullability.ExternalAnnotations
{
    /// <summary>
    /// Performs one-time load of files from built-in Resharper External Annotation folders, along with a cached set of per-assembly External Annotations
    /// (loaded from [AssemblyName].ExternalAnnotations.xml in assembly folder). The annotation files from this last set typically come from NuGet packages
    /// or assembly references. From that set, each per-assembly file is monitored for filesystem changes and flushed accordingly.
    /// </summary>
    public sealed class CachingExternalAnnotationsResolver : IExternalAnnotationsResolver
    {
        [NotNull]
        private readonly AssemblyExternalAnnotationsLoader loader;

        [NotNull]
        private readonly IFileSystem fileSystem;

        [NotNull]
        private readonly ICacheProvider<ExternalAnnotationsMap> cacheProvider;

        [NotNull]
        private readonly ConcurrentDictionary<string, AssemblyCacheEntry> assemblyCache =
            new ConcurrentDictionary<string, AssemblyCacheEntry>(StringComparer.OrdinalIgnoreCase);

        public CachingExternalAnnotationsResolver([NotNull] IFileSystem fileSystem, [NotNull] ICacheProvider<ExternalAnnotationsMap> cacheProvider)
        {
            Guard.NotNull(fileSystem, nameof(fileSystem));
            Guard.NotNull(cacheProvider, nameof(cacheProvider));

            this.fileSystem = fileSystem;
            this.cacheProvider = cacheProvider;

            loader = new AssemblyExternalAnnotationsLoader(fileSystem);
        }

        public void EnsureScanned()
        {
            cacheProvider.GetValue();
        }

        public bool HasAnnotationForSymbol(ISymbol symbol, bool appliesToItem, Compilation compilation)
        {
            Guard.NotNull(symbol, nameof(symbol));
            Guard.NotNull(compilation, nameof(compilation));

            return HasAnnotationInSharedCache(symbol, appliesToItem) || HasAnnotationInSideBySideFile(symbol, appliesToItem, compilation);
        }

        private bool HasAnnotationInSharedCache([NotNull] ISymbol symbol, bool appliesToItem)
        {
            return cacheProvider.GetValue().Contains(symbol, appliesToItem);
        }

        private bool HasAnnotationInSideBySideFile([NotNull] ISymbol symbol, bool appliesToItem, [NotNull] Compilation compilation)
        {
            string path = loader.GetPathForExternalSymbolOrNull(symbol, compilation);

            if (path != null)
            {
                AssemblyCacheEntry entry = assemblyCache.GetOrAdd(path, CreateAssemblyCacheEntry);
                return entry.Map.Contains(symbol, appliesToItem);
            }

            return false;
        }

        public bool IsFileInSideBySideCache([NotNull] string path)
        {
            Guard.NotNull(path, nameof(path));

            return assemblyCache.ContainsKey(path);
        }

        [NotNull]
        private AssemblyCacheEntry CreateAssemblyCacheEntry([NotNull] string path)
        {
            ExternalAnnotationsMap assemblyAnnotationsMap = loader.ParseFile(path);
            IFileSystemWatcher fileWatcher = CreateAssemblyAnnotationsFileWatcher(path);

            return new AssemblyCacheEntry(assemblyAnnotationsMap, fileWatcher);
        }

        [NotNull]
        private IFileSystemWatcher CreateAssemblyAnnotationsFileWatcher([NotNull] string path)
        {
            string directoryName = Path.GetDirectoryName(path);

            if (directoryName == null)
            {
                throw new InvalidOperationException($"Internal error: failed to extract directory from path '{path}'.");
            }

            string filter = Path.GetFileName(path);
            IFileSystemWatcher assemblyAnnotationsFileWatcher = fileSystem.ConstructFileSystemWatcher(directoryName, filter);

            assemblyAnnotationsFileWatcher.Changed += WatcherOnChanged;
            assemblyAnnotationsFileWatcher.Created += WatcherOnChanged;
            assemblyAnnotationsFileWatcher.Deleted += WatcherOnChanged;
            assemblyAnnotationsFileWatcher.Renamed += (s, e) => WatcherOnChanged(s, OldValuesFrom(e));

            assemblyAnnotationsFileWatcher.EnableRaisingEvents = true;
            return assemblyAnnotationsFileWatcher;
        }

        private void WatcherOnChanged([NotNull] object sender, [NotNull] FileSystemEventArgs e)
        {
            if (assemblyCache.TryRemove(e.FullPath, out AssemblyCacheEntry existing))
            {
                existing.Watcher.EnableRaisingEvents = false;
                existing.Watcher.Dispose();
            }
        }

        [NotNull]
        private static FileSystemEventArgs OldValuesFrom([NotNull] RenamedEventArgs e)
        {
            string directoryName = Path.GetDirectoryName(e.OldFullPath);

            if (directoryName == null)
            {
                throw new InvalidOperationException($"Internal error: failed to extract directory from path '{e.OldFullPath}'.");
            }

            return new FileSystemEventArgs(e.ChangeType, directoryName, e.OldName);
        }

        private sealed class AssemblyCacheEntry
        {
            [NotNull]
            public ExternalAnnotationsMap Map { get; }

            [NotNull]
            public IFileSystemWatcher Watcher { get; }

            public AssemblyCacheEntry([NotNull] ExternalAnnotationsMap map, [NotNull] IFileSystemWatcher watcher)
            {
                Guard.NotNull(map, nameof(map));
                Guard.NotNull(watcher, nameof(watcher));

                Map = map;
                Watcher = watcher;
            }
        }
    }
}
