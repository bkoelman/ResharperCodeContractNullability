using JetBrains.Annotations;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Indicates whether an annotation has been found up the type hierarchy. The value <c>null</c> means that no higher type (base class
    /// or interface) was found.
    /// </summary>
    public struct TypeHierarchyLookupResult
    {
        [CanBeNull]
        public bool? IsAnnotatedAtHigherLevelInSource { get; }

        [CanBeNull]
        public bool? IsAnnotatedAtHigherLevelInAssembly { get; }

        public bool HasAnnotation => IsAnnotatedAtHigherLevelInSource == true || IsAnnotatedAtHigherLevelInAssembly == true;

        public static TypeHierarchyLookupResult ForAnnotated(bool isInExternalAssembly)
        {
            return isInExternalAssembly ? new TypeHierarchyLookupResult(null, true) : new TypeHierarchyLookupResult(true, null);
        }

        public static TypeHierarchyLookupResult ForNonAnnotated(bool higherLevelExistsInSource, bool higherLevelExistsInAssembly)
        {
            return new TypeHierarchyLookupResult(higherLevelExistsInSource ? (bool?)false : null,
                higherLevelExistsInAssembly ? (bool?)false : null);
        }

        private TypeHierarchyLookupResult([CanBeNull] bool? isAnnotatedAtHigherLevelInSource,
            [CanBeNull] bool? isAnnotatedAtHigherLevelInAssembly)
        {
            IsAnnotatedAtHigherLevelInSource = isAnnotatedAtHigherLevelInSource;
            IsAnnotatedAtHigherLevelInAssembly = isAnnotatedAtHigherLevelInAssembly;
        }

        public static TypeHierarchyLookupResult Merge(TypeHierarchyLookupResult left, TypeHierarchyLookupResult right)
        {
            bool? sourceResult = MergeValue(left.IsAnnotatedAtHigherLevelInSource, right.IsAnnotatedAtHigherLevelInSource);
            bool? assemblyResult = MergeValue(left.IsAnnotatedAtHigherLevelInAssembly, right.IsAnnotatedAtHigherLevelInAssembly);

            return new TypeHierarchyLookupResult(sourceResult, assemblyResult);
        }

        [CanBeNull]
        private static bool? MergeValue([CanBeNull] bool? left, [CanBeNull] bool? right)
        {
            if (left == true || right == true)
            {
                return true;
            }

            if (left == false || right == false)
            {
                return false;
            }

            return null;
        }

        public override string ToString()
        {
            return $"Source={IsAnnotatedAtHigherLevelInSource}, Assembly={IsAnnotatedAtHigherLevelInAssembly}";
        }
    }
}
