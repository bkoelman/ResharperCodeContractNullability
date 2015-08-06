using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace CodeContractNullability.Utilities
{
    /// <summary>
    /// Member precondition checks.
    /// </summary>
    public static class Guard
    {
        [AssertionMethod]
        [ContractAnnotation("value: null => halt")]
        [DebuggerStepThrough]
        public static void NotNull<T>([CanBeNull] [NoEnumeration] T value, [NotNull] [InvokerParameterName] string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        [AssertionMethod]
        [DebuggerStepThrough]
        public static void HasCount<T>([NotNull] [ItemCanBeNull] IEnumerable<T> value,
            [InvokerParameterName] [NotNull] string name, int length)
        {
            IEnumerable<T> firstItems = value.Take(length + 1);
            if (firstItems.Count() != length)
            {
                throw new ArgumentOutOfRangeException(name, value, $"{name} must have length {length}.");
            }
        }
    }
}