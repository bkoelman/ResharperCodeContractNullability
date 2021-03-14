using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace CodeContractNullability.Utilities
{
    /// <summary>
    /// Logs the execution duration of a code block for diagnostics purposes.
    /// </summary>
    internal sealed class CodeTimer : IDisposable
    {
        [NotNull]
        private readonly string text;

        [NotNull]
        private readonly Stopwatch stopwatch = new Stopwatch();

        public CodeTimer([NotNull] string text)
        {
            Guard.NotNull(text, nameof(text));

            this.text = text;
            stopwatch.Start();
        }

        public void Dispose()
        {
            stopwatch.Stop();
            Debug.WriteLine($"Duration of {text}: {stopwatch.ElapsedMilliseconds} msec");
        }
    }
}
