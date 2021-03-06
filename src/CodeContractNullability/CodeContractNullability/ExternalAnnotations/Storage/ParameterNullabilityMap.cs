﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations.Storage
{
    /// <summary>
    /// Data storage for external annotations.
    /// </summary>
    [CollectionDataContract(Name = "p", ItemName = "e", KeyName = "k", ValueName = "v", Namespace = ExternalAnnotationsCache.CacheNamespace)]
    [Serializable]
    public sealed class ParameterNullabilityMap : Dictionary<string, bool>
    {
        public ParameterNullabilityMap()
        {
        }

        private ParameterNullabilityMap([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
