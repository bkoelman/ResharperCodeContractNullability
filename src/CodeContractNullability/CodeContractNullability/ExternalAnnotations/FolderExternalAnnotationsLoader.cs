using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using CodeContractNullability.ExternalAnnotations.Storage;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations
{
    /// <summary>
    /// Scans the filesystem for Resharper external annotations in xml files.
    /// </summary>
    /// <remarks>
    /// Resharper provides downloadable xml definitions that contain decoration of built-in .NET Framework types. When a class
    /// derives from such a built-in type, we need to have those definitions available because Resharper reports nullability
    /// annotation as unneeded when a base type is already decorated.
    /// </remarks>
    public static class FolderExternalAnnotationsLoader
    {
        [NotNull]
        private static readonly string CachePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"CodeContractNullabilityAnalyzer\external-annotations-cache.xml");

        // Prevents IOException (process cannot access file) when host executes analyzers in parallel.
        [NotNull]
        private static readonly object LockObject = new object();

        [NotNull]
        public static ExternalAnnotationsMap Create()
        {
            try
            {
                ExternalAnnotationsMap map = GetCached();
                if (map.Count > 0)
                {
                    return map;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load Resharper external annotations: {ex.Message}", ex);
            }

            throw new Exception("Failed to load Resharper external annotations.");
        }

        [NotNull]
        private static ExternalAnnotationsMap GetCached()
        {
            lock (LockObject)
            {
                ExternalAnnotationsCache cached = TryGetCacheFromDisk();
                DateTime highestLastWriteTimeUtcOnDisk = cached != null
                    ? GetHighestLastWriteTimeUtc()
                    : DateTime.MinValue;

                if (cached == null || cached.LastWriteTimeUtc < highestLastWriteTimeUtcOnDisk)
                {
                    cached = ScanForMemberExternalAnnotations();
                    SaveToDisk(cached);
                }

                return cached.ExternalAnnotations;
            }
        }

        [CanBeNull]
        private static ExternalAnnotationsCache TryGetCacheFromDisk()
        {
            try
            {
                if (File.Exists(CachePath))
                {
                    var serializer = new DataContractSerializer(typeof (ExternalAnnotationsCache));
                    using (FileStream stream = File.OpenRead(CachePath))
                    {
                        return (ExternalAnnotationsCache) serializer.ReadObject(stream);
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (XmlException)
            {
            }

            return null;
        }

        private static DateTime GetHighestLastWriteTimeUtc()
        {
            var recorder = new HighestLastWriteTimeUtcRecorder();
            foreach (string path in EnumerateAnnotationFiles())
            {
                recorder.VisitFile(path);
            }

            return recorder.HighestLastWriteTimeUtc;
        }

        private static void SaveToDisk([NotNull] ExternalAnnotationsCache cache)
        {
            EnsureDirectoryExists();

            var serializer = new DataContractSerializer(typeof (ExternalAnnotationsCache));
            using (FileStream stream = File.Create(CachePath))
            {
                serializer.WriteObject(stream, cache);
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
            return new ExternalAnnotationsCache(recorder.HighestLastWriteTimeUtc, result);
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> EnumerateAnnotationFiles()
        {
            string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var foldersToScan = new[]
            {
                Path.Combine(localAppDataFolder, ExternalAnnotationFolders.Resharper9ForVisualStudio2015),
                Path.Combine(localAppDataFolder, ExternalAnnotationFolders.Resharper9ExtensionsForVisualStudio2015)
            };

            var fileSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string folder in foldersToScan)
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

            public void VisitFile([NotNull] string path)
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.LastWriteTimeUtc > HighestLastWriteTimeUtc)
                {
                    HighestLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                }
            }
        }

        private static class ExternalAnnotationFolders
        {
            public const string Resharper9ForVisualStudio2015 =
                @"JetBrains\Installations\ReSharperPlatformVs14\ExternalAnnotations";

            public const string Resharper9ExtensionsForVisualStudio2015 =
                @"JetBrains\Installations\ReSharperPlatformVs14\Extensions";
        }
    }
}