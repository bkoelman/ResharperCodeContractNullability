using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using MsgPack.Serialization;

namespace CodeContractNullability.ExternalAnnotations
{
    /// <summary>
    /// Scans the filesystem for Resharper external annotations in xml files.
    /// </summary>
    /// <remarks>
    /// Resharper provides downloadable xml definitions that contain decoration of built-in .NET Framework types. When a class derives
    /// from such a built-in type, we need to have those definitions available because Resharper reports nullability annotation as
    /// unneeded when a base type is already decorated.
    /// </remarks>
    internal static class FolderExternalAnnotationsLoader
    {
        [NotNull]
        private static readonly FolderOnDiskScanner Scanner = new FolderOnDiskScanner();

        [NotNull]
        private static readonly string CachePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ResharperCodeContractNullability", "external-annotations.cache");

        [NotNull]
        private static readonly object LockObject = new object();

        [NotNull]
        public static ExternalAnnotationsMap Create()
        {
            // The lock prevents IOException (process cannot access file) when host executes analyzers in parallel.
            lock (LockObject)
            {
                try
                {
                    return GetCached();
                }
                catch (MissingExternalAnnotationsException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw GetErrorForMissingExternalAnnotations(ex);
                }
            }
        }

        [NotNull]
        private static Exception GetErrorForMissingExternalAnnotations([CanBeNull] Exception error = null)
        {
            IEnumerable<string> folderSet = Scanner.GetFoldersToProbe();
            string folders = string.Join(";", folderSet.Select(SurroundWithDoubleQuotes));

            string message = "Failed to load Resharper external annotations. Scanned folders: " + folders;
            return new MissingExternalAnnotationsException(message, error);
        }

        [NotNull]
        private static string SurroundWithDoubleQuotes([NotNull] string path)
        {
            return "\"" + path + "\"";
        }

        [NotNull]
        private static ExternalAnnotationsMap GetCached()
        {
            ExternalAnnotationsCache cached = TryGetCacheFromDisk();
            DateTime highestLastWriteTimeUtcOnDisk = cached != null ? GetHighestLastWriteTimeUtc() : DateTime.MinValue;

            if (cached == null || cached.LastWriteTimeUtc != highestLastWriteTimeUtcOnDisk)
            {
                using (new CodeTimer("ExternalAnnotationsCache:Create"))
                {
                    cached = ScanForMemberExternalAnnotations();
                    TrySaveToDisk(cached);
                }
            }

            return cached.ExternalAnnotations;
        }

        [CanBeNull]
        private static ExternalAnnotationsCache TryGetCacheFromDisk()
        {
            try
            {
                if (File.Exists(CachePath))
                {
                    MessagePackSerializer<ExternalAnnotationsCache> serializer =
                        SerializationContext.Default.GetSerializer<ExternalAnnotationsCache>();
                    using (FileStream stream = File.OpenRead(CachePath))
                    {
                        using (new CodeTimer("ExternalAnnotationsCache:Read"))
                        {
                            ExternalAnnotationsCache result = serializer.Unpack(stream);

                            if (result.ExternalAnnotations.Any())
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }

            return null;
        }

        private static DateTime GetHighestLastWriteTimeUtc()
        {
            using (new CodeTimer("ExternalAnnotationsCache:Scan"))
            {
                var recorder = new HighestLastWriteTimeUtcRecorder();
                foreach (string path in EnumerateAnnotationFiles())
                {
                    recorder.VisitFile(path);
                }

                if (!recorder.HasSeenFiles)
                {
                    throw GetErrorForMissingExternalAnnotations();
                }

                return recorder.HighestLastWriteTimeUtc;
            }
        }

        private static void TrySaveToDisk([NotNull] ExternalAnnotationsCache cache)
        {
            try
            {
                EnsureDirectoryExists();

                MessagePackSerializer<ExternalAnnotationsCache> serializer =
                    SerializationContext.Default.GetSerializer<ExternalAnnotationsCache>();
                using (FileStream stream = File.Create(CachePath))
                {
                    using (new CodeTimer("ExternalAnnotationsCache:Write"))
                    {
                        serializer.Pack(stream, cache);
                    }
                }
            }
            catch (IOException)
            {
                // When MSBuild runs in parallel, another process may be writing the cache file at the same time.
            }
        }

        private static void EnsureDirectoryExists()
        {
            string folder = Path.GetDirectoryName(CachePath);
            if (folder != null)
            {
                Directory.CreateDirectory(folder);
            }
        }

        [NotNull]
        private static ExternalAnnotationsCache ScanForMemberExternalAnnotations()
        {
            var result = new ExternalAnnotationsMap();
            var parser = new ExternalAnnotationDocumentParser();
            var recorder = new HighestLastWriteTimeUtcRecorder();

            foreach (string path in EnumerateAnnotationFiles())
            {
                recorder.VisitFile(path);

                using (StreamReader reader = File.OpenText(path))
                {
                    parser.ProcessDocument(reader, result);
                }
            }

            Compact(result);

            if (!result.Any())
            {
                throw GetErrorForMissingExternalAnnotations();
            }

            return new ExternalAnnotationsCache(recorder.HighestLastWriteTimeUtc, result);
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> EnumerateAnnotationFiles()
        {
            var fileSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string folder in Scanner.GetFoldersToScan())
            {
                if (Directory.Exists(folder))
                {
                    foreach (string path in Directory.EnumerateFiles(folder, "*.xml", SearchOption.AllDirectories))
                    {
                        fileSet.Add(path);
                    }
                }
            }

            return fileSet;
        }

        private static void Compact([NotNull] ExternalAnnotationsMap externalAnnotations)
        {
            foreach (string key in externalAnnotations.Keys.ToList())
            {
                MemberNullabilityInfo annotation = externalAnnotations[key];
                if (!HasNullabilityDefined(annotation))
                {
                    externalAnnotations.Remove(key);
                }
            }
        }

        private static bool HasNullabilityDefined([NotNull] MemberNullabilityInfo info)
        {
            return info.HasNullabilityDefined || info.ParametersNullability.Count > 0;
        }

        private sealed class HighestLastWriteTimeUtcRecorder
        {
            public DateTime HighestLastWriteTimeUtc { get; private set; }

            public bool HasSeenFiles => HighestLastWriteTimeUtc > DateTime.MinValue;

            public void VisitFile([NotNull] string path)
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.LastWriteTimeUtc > HighestLastWriteTimeUtc)
                {
                    HighestLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                }
            }
        }
    }
}
