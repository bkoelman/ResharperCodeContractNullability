using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace CodeContractNullability
{
    [DataContract(Namespace = SettingsNamespace)]
    [Serializable]
    public sealed class AnalyzerSettings
    {
        internal const string SettingsNamespace = "ResharperCodeContractNullabilitySettings";

        [DataMember(Name = "disableReportOnNullableValueTypes")]
        public bool DisableReportOnNullableValueTypes { get; private set; }

        [NotNull]
        public static AnalyzerSettings Default = new AnalyzerSettings();

        public AnalyzerSettings()
        {
        }

        public AnalyzerSettings(bool disableReportOnNullableValueTypes)
        {
            DisableReportOnNullableValueTypes = disableReportOnNullableValueTypes;
        }
    }
}