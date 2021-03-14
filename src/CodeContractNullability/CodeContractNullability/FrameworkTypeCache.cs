using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability
{
    internal sealed class FrameworkTypeCache
    {
        [NotNull]
        private readonly Compilation compilation;

        [NotNull]
        private readonly ConcurrentDictionary<string, INamedTypeSymbol> typeMap = new();

        [CanBeNull]
        public INamedTypeSymbol EnumerableOfT => GetCached(typeof(IEnumerable<>));

        [CanBeNull]
        public INamedTypeSymbol Enumerable => GetCached(typeof(IEnumerable));

        [CanBeNull]
        public INamedTypeSymbol String => GetCached(typeof(string));

        [CanBeNull]
        public INamedTypeSymbol Object => GetCached(typeof(object));

        [CanBeNull]
        public INamedTypeSymbol LazyOfT => GetCached(typeof(Lazy<>));

        [CanBeNull]
        public INamedTypeSymbol TaskOfT => GetCached(typeof(Task<>));

        [CanBeNull]
        public INamedTypeSymbol ValueTaskOfT => GetCached("System.Threading.Tasks.ValueTask`1");

        [CanBeNull]
        public INamedTypeSymbol CompilerGeneratedAttribute => GetCached(typeof(CompilerGeneratedAttribute));

        [CanBeNull]
        public INamedTypeSymbol DebuggerNonUserCodeAttribute => GetCached(typeof(DebuggerNonUserCodeAttribute));

        [CanBeNull]
        public INamedTypeSymbol ConditionalAttribute => GetCached(typeof(ConditionalAttribute));

        public FrameworkTypeCache([NotNull] Compilation compilation)
        {
            Guard.NotNull(compilation, nameof(compilation));
            this.compilation = compilation;
        }

        [CanBeNull]
        private INamedTypeSymbol GetCached([NotNull] Type type)
        {
            string typeName = type.FullName;

            if (typeName == null)
            {
                throw new InvalidOperationException($"Internal error: failed to resolve full name of type '{type}'.");
            }

            return GetCached(typeName);
        }

        [CanBeNull]
        private INamedTypeSymbol GetCached([NotNull] string typeName)
        {
            return typeMap.GetOrAdd(typeName, _ => compilation.GetTypeByMetadataName(typeName));
        }
    }
}
