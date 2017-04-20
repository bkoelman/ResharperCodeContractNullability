using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace CodeContractNullability.Settings
{
    [DataContract(Namespace = SettingsNamespace)]
    [Serializable]
    public sealed class AnalyzerSettings
    {
        private const string SettingsNamespace = "ResharperCodeContractNullabilitySettings";

        [DataMember(Name = "disableReportOnNullableValueTypes")]
        public bool DisableReportOnNullableValueTypes { get; private set; }

        [DataMember(Name = "typeHierarchyReportMode", IsRequired = false)]
        public TypeHierarchyReportMode TypeHierarchyReportMode { get; private set; }

        [NotNull]
        public static readonly AnalyzerSettings Default = new AnalyzerSettings();

        public AnalyzerSettings()
        {
        }

        public AnalyzerSettings(bool disableReportOnNullableValueTypes, TypeHierarchyReportMode typeHierarchyReportMode)
        {
            DisableReportOnNullableValueTypes = disableReportOnNullableValueTypes;
            TypeHierarchyReportMode = typeHierarchyReportMode;
        }

        [NotNull]
        public AnalyzerSettings WithDisableReportOnNullableValueTypes(bool disable)
        {
            return new AnalyzerSettings(disable, TypeHierarchyReportMode);
        }

        [NotNull]
        public AnalyzerSettings InTypeHierarchyReportMode(TypeHierarchyReportMode mode)
        {
            return new AnalyzerSettings(DisableReportOnNullableValueTypes, mode);
        }
    }
}
