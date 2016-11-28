using System.Collections.Generic;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests concerning the interpretation of full metadata name notation of members, as they occur in external annotation
    /// files.
    /// </summary>
    public sealed class ExternalAnnotationMemberNamingSpecs : NullabilityNUnitRoslynTest
    {
        [Fact]
        public void When_property_in_generic_class_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    namespace SampleNamespace
                    {
                        public class SampleClass<T>
                        {
                            public virtual IEnumerable<T> TheEnumerable { get; set; }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:SampleNamespace.SampleClass`1.TheEnumerable")
                        .CanBeNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_variable_length_parameter_in_method_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace TestSystem
                    {
                        public class StringFormatter
                        {
                            public static string Format(IFormatProvider provider, string format, params object[] args)
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named(
                            "M:TestSystem.StringFormatter.Format(System.IFormatProvider,System.String,System.Object[])")
                        .NotNull()
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("provider")
                            .CanBeNull())
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("format")
                            .CanBeNull())
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("args")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_generic_parameters_in_method_of_generic_class_are_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace SystemCollections
                    {
                        public class Dictionary<TKey, TValue>
                        {
                            public void Add(TKey key, TValue value) { throw new NotImplementedException(); }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:SystemCollections.Dictionary`2.Add(`0,`1)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("key")
                            .CanBeNull())
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("value")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_nested_generic_parameters_in_method_of_generic_interface_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    namespace SystemCollections
                    {
                        public interface IDictionary<TKey, TValue>
                        {
                            void SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items);
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named(
                            "M:SystemCollections.IDictionary`2.SetItems(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{`0,`1}})")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("items")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_generic_parameters_in_generic_method_of_nongeneric_class_are_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    namespace SystemCollections
                    {
                        public static class Enumerable
                        {
                            public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named(
                            "M:SystemCollections.Enumerable.SelectMany``2(System.Collections.Generic.IEnumerable{``0},System.Func{``0,System.Collections.Generic.IEnumerable{``1}})")
                        .NotNull()
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("source")
                            .CanBeNull())
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("selector")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_in_nested_class_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace TestSystem
                    {
                        public class Outer
                        {
                            private class Inner
                            {
                                public int? Value;
                            }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("F:TestSystem.Outer.Inner.Value")
                        .CanBeNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_generic_parameters_in_generic_method_in_generic_nested_classes_are_externally_annotated_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(KeyValuePair<,>).Namespace)
                .InGlobalScope(@"
                    namespace TestSystem
                    {
                        public class OuterClass<TOuter1, TOuter2>
                        {
                            public class InnerClass<TInner>
                            {
                                public TMethod1 TestMethod<TMethod1, TMethod2>(TOuter2 testOuter2,
                                    TInner testInner, TMethod2 testMethod2, KeyValuePair<TMethod1, TOuter1> pair)
                                {
                                    throw new NotImplementedException();
                                }
                            }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named(
                            "M:TestSystem.OuterClass`2.InnerClass`1.TestMethod``2(`1,`2,``1,System.Collections.Generic.KeyValuePair{``0,`0})")
                        .CanBeNull()
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("testOuter2")
                            .CanBeNull())
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("testInner")
                            .CanBeNull())
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("testMethod2")
                            .CanBeNull())
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("pair")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }
    }
}
