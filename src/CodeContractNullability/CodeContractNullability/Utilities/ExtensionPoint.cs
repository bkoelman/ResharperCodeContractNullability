using System;
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
        private readonly Func<TInterface> createDefaultInstanceFactory;

        [CanBeNull]
        private TInterface specificInstance;

        [CanBeNull]
        private TInterface activeInstance;

        public ExtensionPoint([NotNull] Func<TInterface> createDefaultInstance)
        {
            Guard.NotNull(createDefaultInstance, nameof(createDefaultInstance));
            createDefaultInstanceFactory = createDefaultInstance;
        }

        [NotNull]
        private static TInterface InstantiateNotNull([NotNull] Func<TInterface> valueFactory)
        {
            TInterface result = valueFactory();
            if (result == null)
            {
                throw new Exception($"Failed to create instance of {typeof (TInterface)}.");
            }
            return result;
        }

        [NotNull]
        public TInterface GetCached()
        {
            if (activeInstance == null)
            {
                activeInstance = specificInstance ?? InstantiateNotNull(createDefaultInstanceFactory);
            }
            return activeInstance;
        }

        public void Override([NotNull] TInterface instance)
        {
            Guard.NotNull(instance, nameof(instance));

            specificInstance = instance;
        }
    }
}