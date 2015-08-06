using System.Runtime.Serialization;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.ExternalAnnotations.Storage
{
    /// <summary>
    /// Data storage for external annotations.
    /// </summary>
    [DataContract(Name = "i", Namespace = ExternalAnnotationsCache.CacheNamespace)]
    public class MemberNullabilityInfo
    {
        [DataMember(Name = "t")]
        [NotNull]
        public string Type { get; private set; }

        [DataMember(Name = "n")]
        private int InnerHasNullabilityDefined { get; set; }

        public bool HasNullabilityDefined
        {
            get
            {
                return InnerHasNullabilityDefined != 0;
            }
            set
            {
                InnerHasNullabilityDefined = value ? 1 : 0;
            }
        }

        [DataMember(Name = "p")]
        [NotNull]
        public ParameterNullabilityMap ParametersNullability { get; private set; }

        public MemberNullabilityInfo([NotNull] string type)
        {
            Guard.NotNull(type, nameof(type));

            Type = type;
            ParametersNullability = new ParameterNullabilityMap();
            HasNullabilityDefined = false;
        }
    }
}