using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations
{
    internal sealed class FolderOnDiskScanner
    {
        private const string ResharperFolderNamePrefix = "ReSharperPlatformVs";

        [NotNull]
        private readonly string programFilesX86Folder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        [NotNull]
        private readonly string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        [NotNull]
        private readonly string nuGetDirectory =
            Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.nuget\packages\jetbrains.externalannotations");

        [NotNull]
        private static readonly Scope[] Scopes = Enum.GetValues(typeof(Scope)).Cast<Scope>().ToArray();

        [NotNull]
        private static readonly Category[] Categories = Enum.GetValues(typeof(Category)).Cast<Category>().ToArray();

        [NotNull]
        [ItemNotNull]
        public IEnumerable<string> GetFoldersToScan()
        {
            foreach (ExternalAnnotationsLocation location in
                from folder in EnumerateExternalAnnotationLocations()
                orderby folder.Category, folder.Scope, folder.Version
                select folder)
            {
                yield return location.Path;
            }
        }

        [NotNull]
        [ItemNotNull]
        public IEnumerable<string> GetFoldersToProbe()
        {
            yield return GetResharperProbingFolder(programFilesX86Folder, "ExternalAnnotations");
            yield return GetResharperProbingFolder(programFilesX86Folder, "Extensions");
            yield return GetNuGetProbingFolder(nuGetDirectory);
            yield return GetResharperProbingFolder(localAppDataFolder, "ExternalAnnotations");
            yield return GetResharperProbingFolder(localAppDataFolder, "Extensions");
        }

        [NotNull]
        private static string GetResharperProbingFolder([NotNull] string startFolder, [NotNull] string lastFolder)
        {
            return Path.Combine(startFolder, "JetBrains", "Installations", "ReSharperPlatformVs??", lastFolder);
        }

        [NotNull]
        private static string GetNuGetProbingFolder([NotNull] string nuGetDirectory)
        {
            return Path.Combine(nuGetDirectory, "*", "DotFiles", "ExternalAnnotations");
        }

        [NotNull]
        [ItemNotNull]
        private IEnumerable<ExternalAnnotationsLocation> EnumerateExternalAnnotationLocations()
        {
            foreach (Scope scope in Scopes)
            {
                if (scope == Scope.NuGet)
                {
                    foreach (ExternalAnnotationsLocation location in EnumerateNuGetSubfolders())
                    {
                        yield return location;
                    }
                }
                else
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
        }

        [NotNull]
        [ItemNotNull]
        private IEnumerable<ExternalAnnotationsLocation> EnumerateNuGetSubfolders()
        {
            if (!Directory.Exists(nuGetDirectory))
            {
                yield break;
            }

            const string subFolder = @"DotFiles\ExternalAnnotations";

            foreach (string versionPath in Directory.GetDirectories(nuGetDirectory))
            {
                string versionFolder = Path.GetFileName(versionPath);
                if (versionFolder != null && Version.TryParse(versionFolder, out Version packageVersion))
                {
                    string path = Path.Combine(versionPath, subFolder);
                    if (Directory.Exists(path))
                    {
                        yield return new ExternalAnnotationsLocation(Scope.NuGet, Category.ExternalAnnotations, packageVersion,
                            path);
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
                    if (int.TryParse(platformFolder.Substring(ResharperFolderNamePrefix.Length, 2), out int vsVersion) &&
                        vsVersion >= 14)
                    {
                        string path = Path.Combine(platformPath, subFolder);
                        if (Directory.Exists(path))
                        {
                            yield return new ExternalAnnotationsLocation(scope, category, new Version(vsVersion, 0), path);
                        }
                    }
                }
            }
        }

        private sealed class ExternalAnnotationsLocation
        {
            public Scope Scope { get; }
            public Category Category { get; }

            [NotNull]
            public Version Version { get; }

            [NotNull]
            public string Path { get; }

            public ExternalAnnotationsLocation(Scope scope, Category category, [NotNull] Version version, [NotNull] string path)
            {
                Guard.NotNull(version, nameof(version));
                Guard.NotNull(path, nameof(path));

                Scope = scope;
                Category = category;
                Version = version;
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
            User,
            NuGet
        }
    }
}
