using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeContractNullability
{
    internal static class NullableReferenceTypeSupport
    {
        [NotNull]
        [ItemNotNull]
        private static readonly object[] EmptyObjectArray = new object[0];

        [NotNull]
        [ItemCanBeNull]
        private static readonly Lazy<PropertyInfo> LazyNullableContextOptionsProperty =
            new Lazy<PropertyInfo>(() => typeof(CSharpCompilationOptions).GetProperty("NullableContextOptions"),
                LazyThreadSafetyMode.PublicationOnly);

        public static bool IsActive([NotNull] Compilation compilation)
        {
            ParseOptions optionsOrNull = compilation.SyntaxTrees.FirstOrDefault()?.Options;

            return optionsOrNull != null && IsLanguageVersionEightOrHigher(optionsOrNull) &&
                IsNullableAnnotationContextEnabled(compilation.Options);
        }

        private static bool IsLanguageVersionEightOrHigher([NotNull] ParseOptions parseOptions)
        {
            return ((CSharpParseOptions)parseOptions).LanguageVersion >= (LanguageVersion)8;
        }

        private static bool IsNullableAnnotationContextEnabled([NotNull] CompilationOptions compilationOptions)
        {
            PropertyInfo property = LazyNullableContextOptionsProperty.Value;

            if (property != null)
            {
                string enumText = property.GetGetMethod().Invoke(compilationOptions, EmptyObjectArray).ToString();
                return enumText == "Enable" || enumText == "SafeOnly";
            }

            return false;
        }
    }
}
