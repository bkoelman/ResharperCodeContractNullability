using System;
using System.Runtime.Serialization;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations.Storage
{
    /// <summary>
    /// Represents the external annotations cache file, stored in compact form.
    /// </summary>
    [DataContract(Namespace = CacheNamespace)]
    [Serializable]
    public class ExternalAnnotationsCache
    {
        internal const string CacheNamespace = "CodeContractNullability";

        [DataMember(Name = "lastWriteTimeUtc")]
        public DateTime LastWriteTimeUtc { get; private set; }

        [DataMember(Name = "annotations")]
        [NotNull]
        public ExternalAnnotationsMap ExternalAnnotations { get; private set; }

        public ExternalAnnotationsCache()
        {
            ExternalAnnotations = new ExternalAnnotationsMap();
        }

        public ExternalAnnotationsCache(DateTime lastWriteTimeUtc, [NotNull] ExternalAnnotationsMap externalAnnotations)
        {
            Guard.NotNull(externalAnnotations, nameof(externalAnnotations));

            LastWriteTimeUtc = lastWriteTimeUtc;
            ExternalAnnotations = externalAnnotations;
        }
    }
}
