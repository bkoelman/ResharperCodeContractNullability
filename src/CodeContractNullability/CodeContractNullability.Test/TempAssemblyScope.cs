using System;
using System.IO;
using JetBrains.Annotations;

namespace CodeContractNullability.Test
{
#pragma warning disable FS01 // Usage of non-testable file system.
    internal sealed class TempAssemblyScope : IDisposable
    {
        [NotNull]
        public string TempPath { get; }

        [NotNull]
        public string AssemblyPath => TempPath + ".dll";

        public TempAssemblyScope()
        {
            TempPath = Path.GetTempFileName();
        }

        public void Dispose()
        {
            File.Delete(AssemblyPath);
            File.Delete(TempPath);
        }
    }
#pragma warning restore FS01 // Usage of non-testable file system.
}
