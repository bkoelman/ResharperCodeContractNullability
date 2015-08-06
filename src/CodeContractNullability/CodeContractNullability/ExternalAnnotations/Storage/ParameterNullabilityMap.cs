using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CodeContractNullability.ExternalAnnotations.Storage
{
    /// <summary>
    /// Data storage for external annotations.
    /// </summary>
    [CollectionDataContract(Name = "p", ItemName = "e", KeyName = "k", ValueName = "v",
        Namespace = ExternalAnnotationsCache.CacheNamespace)]
    public class ParameterNullabilityMap : Dictionary<string, bool>
    {
    }
}