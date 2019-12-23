using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace CodeContractNullability
{
    [DataContract(Namespace = SettingsNamespace)]
    [Serializable]
    public sealed class AnalyzerSettings
    {
        private const string SettingsNamespace = "ResharperCodeContractNullabilitySettings";

        [NotNull]
        internal static readonly AnalyzerSettings Default = new AnalyzerSettings();

        [DataMember(Name = "disableReportOnNullableValueTypes")]
        public bool DisableReportOnNullableValueTypes { get; private set; }

        public AnalyzerSettings()
        {
        }

        public AnalyzerSettings(bool disableReportOnNullableValueTypes)
        {
            DisableReportOnNullableValueTypes = disableReportOnNullableValueTypes;
        }
    }
}
