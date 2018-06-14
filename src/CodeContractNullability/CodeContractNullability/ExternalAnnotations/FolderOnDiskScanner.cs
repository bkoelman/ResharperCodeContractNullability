using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations
{
    public sealed class FolderOnDiskScanner
    {
        private const string ResharperFolderNamePrefix = "ReSharperPlatformVs";

        [NotNull]
        private readonly string programFilesX86Folder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        [NotNull]
        private readonly string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        [NotNull]
        private static readonly Scope[] Scopes =
        {
            Scope.System,
            Scope.User
        };

        [NotNull]
        private static readonly Category[] Categories =
        {
            Category.ExternalAnnotations,
            Category.Extensions
        };

        [NotNull]
        [ItemNotNull]
        public IEnumerable<string> GetFoldersToScan()
        {
            foreach (ExternalAnnotationsLocation location in
                from folder in EnumerateExternalAnnotationLocations()
                orderby folder.Category, folder.Scope, folder.VsVersion
                select folder)
            {
                yield return location.Path;
            }
        }

        [NotNull]
        [ItemNotNull]
        public IEnumerable<string> GetFoldersToProbe()
        {
            yield return GetProbingFolder(programFilesX86Folder, "ExternalAnnotations");
            yield return GetProbingFolder(programFilesX86Folder, "Extensions");
            yield return GetProbingFolder(localAppDataFolder, "ExternalAnnotations");
            yield return GetProbingFolder(localAppDataFolder, "Extensions");
        }

        [NotNull]
        private static string GetProbingFolder([NotNull] string startFolder, [NotNull] string lastFolder)
        {
            return Path.Combine(startFolder, "JetBrains", "Installations", "ReSharperPlatformVs??", lastFolder);
        }

        [NotNull]
        [ItemNotNull]
        private IEnumerable<ExternalAnnotationsLocation> EnumerateExternalAnnotationLocations()
        {
            foreach (Scope scope in Scopes)
            {
                foreach (Category category in Categories)
                {
                    foreach (ExternalAnnotationsLocation location in EnumerateResharperSubfolders(scope, category))
                    {
                        yield return location;
                    }
                }
            }
        }

        [NotNull]
        [ItemNotNull]
        private IEnumerable<ExternalAnnotationsLocation> EnumerateResharperSubfolders(Scope scope, Category category)
        {
            string startFolder = scope == Scope.System ? programFilesX86Folder : localAppDataFolder;
            string subFolder = category == Category.ExternalAnnotations ? "ExternalAnnotations" : "Extensions";

            string installationsFolder = Path.Combine(startFolder, "JetBrains", "Installations");

            if (!Directory.Exists(installationsFolder))
            {
                yield break;
            }

            foreach (string platformPath in Directory.GetDirectories(installationsFolder, ResharperFolderNamePrefix + "*"))
            {
                string platformFolder = Path.GetFileName(platformPath);
                if (platformFolder?.Length >= ResharperFolderNamePrefix.Length + 2)
                {
                    if (int.TryParse(platformFolder?.Substring(ResharperFolderNamePrefix.Length, 2), out int vsVersion) &&
                        vsVersion >= 14)
                    {
                        string path = Path.Combine(platformPath, subFolder);
                        if (Directory.Exists(path))
                        {
                            yield return new ExternalAnnotationsLocation(scope, category, vsVersion, path);
                        }
                    }
                }
            }
        }

        private sealed class ExternalAnnotationsLocation
        {
            public Scope Scope { get; }
            public Category Category { get; }
            public int VsVersion { get; }

            [NotNull]
            public string Path { get; }

            public ExternalAnnotationsLocation(Scope scope, Category category, int vsVersion, [NotNull] string path)
            {
                Guard.NotNull(path, nameof(path));

                Scope = scope;
                Category = category;
                VsVersion = vsVersion;
                Path = path;
            }
        }

        private enum Category
        {
            ExternalAnnotations,
            Extensions
        }

        private enum Scope
        {
            System,
            User
        }
    }
}
