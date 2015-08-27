using System;
using System.Threading;
using JetBrains.Annotations;

namespace CodeContractNullability.Utilities
{
    /// <summary>
    /// Provides a mechanism to prevent creating a (potentially expensive) default object and injecting a simpler variant.
    /// Intended to speed up running unit tests.
    /// </summary>
    /// <typeparam name="TInterface">
    /// Type of the object that is replaceble.
    /// </typeparam>
    public class ExtensionPoint<TInterface>
        where TInterface : class
    {
        [NotNull]
        [ItemNotNull]
        private readonly Lazy<TInterface> lazyInstance;

        [CanBeNull]
        private TInterface specificInstance;

        public ExtensionPoint([NotNull] Func<TInterface> createDefaultInstance)
        {
            Guard.NotNull(createDefaultInstance, nameof(createDefaultInstance));

            lazyInstance = new Lazy<TInterface>(() => specificInstance ?? Instantiate(createDefaultInstance),
                LazyThreadSafetyMode.None);
        }

        [NotNull]
        private TInterface Instantiate([NotNull] Func<TInterface> createDefaultInstance)
        {
            TInterface result = createDefaultInstance();
            if (result == null)
            {
                throw new Exception($"Failed to create instance of {typeof (TInterface)}.");
            }
            return result;
        }

        [NotNull]
        public TInterface GetCached()
        {
            return lazyInstance.Value;
        }

        public void Override([NotNull] TInterface instance)
        {
            Guard.NotNull(instance, nameof(instance));

            specificInstance = instance;
        }
    }
}