using System.Collections.Generic;
using System.Runtime.Serialization;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.ExternalAnnotations.Storage
{
    /// <summary>
    /// Data storage for external annotations.
    /// </summary>
    [CollectionDataContract(Name = "annotations", ItemName = "e", KeyName = "k", ValueName = "v",
        Namespace = ExternalAnnotationsCache.CacheNamespace)]
    public class ExternalAnnotationsMap : Dictionary<string, MemberNullabilityInfo>
    {
        public bool Contains<TSymbol>([NotNull] TSymbol symbol, bool appliesToItem) where TSymbol : class, ISymbol
        {
            Guard.NotNull(symbol, nameof(symbol));

            if (appliesToItem)
            {
                // Note: At the time of writing (August 2015), the set of Resharper's external annotations does not
                // include ItemNotNull / ItemCanBeNull elements. But we likely need to add support for them in the future.
                return false;
            }

            if (symbol is IParameterSymbol)
            {
                string methodId = symbol.ContainingSymbol.GetDocumentationCommentId();
                MemberNullabilityInfo memberInfo = TryGetMemberById(methodId);
                return memberInfo != null && memberInfo.ParametersNullability.ContainsKey(symbol.Name) &&
                    memberInfo.ParametersNullability[symbol.Name];
            }
            else
            {
                string id = symbol.GetDocumentationCommentId();
                MemberNullabilityInfo memberInfo = TryGetMemberById(id);
                return memberInfo != null && memberInfo.HasNullabilityDefined;
            }
        }

        [CanBeNull]
        private MemberNullabilityInfo TryGetMemberById([CanBeNull] string id)
        {
            if (!string.IsNullOrEmpty(id) && id[1] == ':')
            {
                // N = namespace, M = method, F = field, E = event, P = property, T = type
                string type = id.Substring(0, 1);
                string key = id.Substring(2);

                if (ContainsKey(key))
                {
                    MemberNullabilityInfo memberInfo = this[key];
                    if (memberInfo.Type == type)
                    {
                        return memberInfo;
                    }
                }
            }

            return null;
        }
    }
}