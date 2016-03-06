using System;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations
{
    [Serializable]
    public class MissingExternalAnnotationsException : Exception
    {
        public MissingExternalAnnotationsException([NotNull] string message, [CanBeNull] Exception innerException)
            : base(message, innerException)
        {
        }
    }
}