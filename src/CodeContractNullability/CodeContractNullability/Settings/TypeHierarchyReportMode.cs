namespace CodeContractNullability.Settings
{
    /// <summary>
    /// Indicates where to report in non-annotated type hierarchies.
    /// </summary>
    public enum TypeHierarchyReportMode
    {
        /// <summary>
        /// Report on all members in the type hierarchy (default).
        /// </summary>
        Always,

        /// <summary>
        /// Only report on highest-level members in the type hierarchy that are defined in source.
        /// </summary>
        HighestInSource,

        /// <summary>
        /// Never report if the root of the type hierarchy is defined in an external assembly.
        /// </summary>
        NeverIfTopLevelInAssembly
    }
}
