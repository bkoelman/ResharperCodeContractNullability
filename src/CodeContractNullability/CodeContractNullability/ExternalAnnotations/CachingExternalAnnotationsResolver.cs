using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.ExternalAnnotations
{
    /// <summary>
    /// Performs one-time load of files from built-in Resharper External Annotation folders, along with a cached set of
    /// per-assembly External Annotations (loaded from [AssemblyName].ExternalAnnotations.xml in assembly folder). The
    /// annotation files from this last set typically come from NuGet packages or assembly references. From that set, each
    /// per-assembly file is monitored for filesystem changes and flushed accordingly.
    /// </summary>
    public class CachingExternalAnnotationsResolver : IExternalAnnotationsResolver
    {
        [NotNull]
        [ItemNotNull]
        private static readonly Lazy<ExternalAnnotationsMap> GlobalCache =
            new Lazy<ExternalAnnotationsMap>(FolderExternalAnnotationsLoader.Create,
                LazyThreadSafetyMode.ExecutionAndPublication);

        [NotNull]
        private readonly ConcurrentDictionary<string, AssemblyCacheEntry> assemblyCache =
            new ConcurrentDictionary<string, AssemblyCacheEntry>();

        public void EnsureScanned()
        {
            // ReSharper disable once UnusedVariable
            ExternalAnnotationsMap dummy = GlobalCache.Value;
        }

        public bool HasAnnotationForSymbol(ISymbol symbol, bool appliesToItem, Compilation compilation)
        {
            Guard.NotNull(symbol, nameof(symbol));
            Guard.NotNull(compilation, nameof(compilation));

            return HasAnnotationInGlobalCache(symbol, appliesToItem) ||
                HasAnnotationInSideBySideFile(symbol, appliesToItem, compilation);
        }

        private bool HasAnnotationInGlobalCache([NotNull] ISymbol symbol, bool appliesToItem)
        {
            return GlobalCache.Value.Contains(symbol, appliesToItem);
        }

        private bool HasAnnotationInSideBySideFile([NotNull] ISymbol symbol, bool appliesToItem,
            [NotNull] Compilation compilation)
        {
            string path = AssemblyExternalAnnotationsLoader.GetPathForExternalSymbolOrNull(symbol, compilation);
            if (path != null)
            {
                AssemblyCacheEntry entry = assemblyCache.GetOrAdd(path, CreateAssemblyCacheEntry);
                return entry.Map.Contains(symbol, appliesToItem);
            }

            return false;
        }

        [NotNull]
        private AssemblyCacheEntry CreateAssemblyCacheEntry([NotNull] string path)
        {
            ExternalAnnotationsMap assemblyAnnotationsMap = AssemblyExternalAnnotationsLoader.ParseFile(path);
            FileSystemWatcher fileWatcher = CreateAssemblyAnnotationsFileWatcher(path);

            return new AssemblyCacheEntry(assemblyAnnotationsMap, fileWatcher);
        }

        [NotNull]
        private FileSystemWatcher CreateAssemblyAnnotationsFileWatcher([NotNull] string path)
        {
            string directoryName = Path.GetDirectoryName(path);
            string filter = Path.GetFileName(path);
            var assemblyAnnotationsFileWatcher = new FileSystemWatcher(directoryName, filter);

            assemblyAnnotationsFileWatcher.Changed += WatcherOnChanged;
            assemblyAnnotationsFileWatcher.Created += WatcherOnChanged;
            assemblyAnnotationsFileWatcher.Deleted += WatcherOnChanged;
            assemblyAnnotationsFileWatcher.Renamed += (s, e) => WatcherOnChanged(s, OldValuesFrom(e));

            assemblyAnnotationsFileWatcher.EnableRaisingEvents = true;
            return assemblyAnnotationsFileWatcher;
        }

        private void WatcherOnChanged([NotNull] object sender, [NotNull] FileSystemEventArgs e)
        {
            AssemblyCacheEntry existing;
            if (assemblyCache.TryRemove(e.FullPath, out existing))
            {
                existing.Watcher.EnableRaisingEvents = false;
                existing.Watcher.Dispose();
            }
        }

        [NotNull]
        private static FileSystemEventArgs OldValuesFrom([NotNull] RenamedEventArgs e)
        {
            string directory = Path.GetDirectoryName(e.OldFullPath);
            return new FileSystemEventArgs(e.ChangeType, directory, e.OldName);
        }

        private sealed class AssemblyCacheEntry
        {
            [NotNull]
            public ExternalAnnotationsMap Map { get; }

            [NotNull]
            public FileSystemWatcher Watcher { get; }

            public AssemblyCacheEntry([NotNull] ExternalAnnotationsMap map, [NotNull] FileSystemWatcher watcher)
            {
                Guard.NotNull(map, nameof(map));
                Guard.NotNull(watcher, nameof(watcher));

                Map = map;
                Watcher = watcher;
            }
        }
    }
}