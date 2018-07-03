﻿using System;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations
{
    [Serializable]
    internal sealed class MissingExternalAnnotationsException : Exception
    {
        public MissingExternalAnnotationsException([NotNull] string message, [CanBeNull] Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
