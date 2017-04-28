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
    public sealed class FrameworkTypeCache
    {
        [NotNull]
        private readonly Compilation compilation;

        [NotNull]
        private readonly ConcurrentDictionary<string, INamedTypeSymbol> typeMap =
            new ConcurrentDictionary<string, INamedTypeSymbol>();

        public FrameworkTypeCache([NotNull] Compilation compilation)
        {
            Guard.NotNull(compilation, nameof(compilation));
            this.compilation = compilation;
        }

        [CanBeNull]
        public INamedTypeSymbol EnumerableOfT => GetCached(typeof(IEnumerable<>).FullName);

        [CanBeNull]
        public INamedTypeSymbol Enumerable => GetCached(typeof(IEnumerable).FullName);

        [CanBeNull]
        public INamedTypeSymbol String => GetCached(typeof(string).FullName);

        [CanBeNull]
        public INamedTypeSymbol Object => GetCached(typeof(object).FullName);

        [CanBeNull]
        public INamedTypeSymbol LazyOfT => GetCached(typeof(Lazy<>).FullName);

        [CanBeNull]
        public INamedTypeSymbol TaskOfT => GetCached(typeof(Task<>).FullName);

        [CanBeNull]
        public INamedTypeSymbol ValueTaskOfT => GetCached("System.Threading.Tasks.ValueTask`1");

        [CanBeNull]
        public INamedTypeSymbol CompilerGeneratedAttribute => GetCached(typeof(CompilerGeneratedAttribute).FullName);

        [CanBeNull]
        public INamedTypeSymbol DebuggerNonUserCodeAttribute => GetCached(typeof(DebuggerNonUserCodeAttribute).FullName);

        [CanBeNull]
        public INamedTypeSymbol ConditionalAttribute => GetCached(typeof(ConditionalAttribute).FullName);

        [CanBeNull]
        private INamedTypeSymbol GetCached([NotNull] string typeName)
        {
            return typeMap.GetOrAdd(typeName, _ => compilation.GetTypeByMetadataName(typeName));
        }
    }
}
