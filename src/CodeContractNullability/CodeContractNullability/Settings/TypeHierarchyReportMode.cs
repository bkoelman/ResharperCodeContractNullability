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
        /// Only report at the highest-level member in the type hierarchy that is defined in source. If the toplevel member is
        /// defined in an external assembly, this reports the highest-level member that is defined in source.
        /// </summary>
        AtHighestSourceInTypeHierarchy,

        /// <summary>
        /// Only report at the toplevel member in the type hierarchy. If the toplevel member is defined in an external assembly,
        /// this reports nothing.
        /// </summary>
        AtTopInTypeHierarchy
    }
}
