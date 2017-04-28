using System.Threading;
using JetBrains.Annotations;

namespace CodeContractNullability.Utilities
{
    /// <summary>
    /// Holds a reference to an object, where reading and writing the wrapped value always atomically returns the latest value.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the wrapped object reference.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// It is strongly recommended to mark <see cref="FreshReference{T}" /> members in your class as <c>readonly</c>, because accidently
    /// replacing a FreshReference object with another FreshReference object defeats the whole purpose of this class.
    /// </para>
    /// <para>
    /// Note that <see cref="FreshReference{T}" /> only guards noncached and atomic exchange of the wrapped object reference. If you need
    /// to access members of the wrapped object reference noncached or atomically, locking is probably a better solution.
    /// </para>
    /// </remarks>
    public sealed class FreshReference<T>
        where T : class
    {
        [CanBeNull]
        private T innerValue;

        [CanBeNull]
        public T Value
        {
            get => Interlocked.CompareExchange(ref innerValue, null, null);
            set => Interlocked.Exchange(ref innerValue, value);
        }

        public FreshReference([CanBeNull] T value)
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            Value = value;
        }
    }
}
