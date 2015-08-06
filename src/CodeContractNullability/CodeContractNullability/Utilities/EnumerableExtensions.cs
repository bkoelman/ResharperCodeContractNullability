using System.Collections.Generic;
using JetBrains.Annotations;

namespace CodeContractNullability.Utilities
{
    public static class EnumerableExtensions
    {
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<T> PrependIfNotNull<T>([NotNull] [ItemNotNull] this IEnumerable<T> source,
            [CanBeNull] T firstElement)
        {
            Guard.NotNull(source, nameof(source));

            if (firstElement != null)
            {
                yield return firstElement;
            }

            foreach (T item in source)
            {
                yield return item;
            }
        }
    }
}