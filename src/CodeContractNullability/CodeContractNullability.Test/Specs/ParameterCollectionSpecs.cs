using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting item nullability diagnostics on method parameters.
    /// </summary>
    public sealed class ParameterCollectionSpecs : ItemNullabilityTest
    {
        [Fact]
        public void When_parameter_is_annotated_with_item_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        void M([ItemNotNull] IEnumerable<string> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_is_annotated_with_item_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        void M([ItemCanBeNull] IEnumerable<string> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_item_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(List<>).Namespace)
                .InDefaultClass(@"
                    public void M(List<int> p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_item_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        void M(IEnumerable<T> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_item_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .Using(typeof(BindingFlags).Namespace)
                .InDefaultClass(@"
                    void M(IEnumerable<BindingFlags> p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_item_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    void M(<annotate/> IEnumerable<int?> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_item_type_is_nullable_but_analysis_is_disabled_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .DisableReportOnNullableValueTypes)
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    void M(IEnumerable<int?> p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_item_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        void M(<annotate/> IEnumerable<T?> [|p|]) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_item_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    void M(<annotate/> IEnumerable<string> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_item_type_is_object_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable).Namespace)
                .InDefaultClass(@"
                    void M(<annotate/> IEnumerable [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_indexer_parameter_is_collection_of_reference_type_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    int this[<annotate/> IEnumerable<string> [|p|]]
                    {
                        get { throw new NotImplementedException(); }
                        set { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_ref_parameter_is_collection_of_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    void M(<annotate/> ref IEnumerable<int?> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_ref_parameter_is_collection_of_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    void M(ref IEnumerable<int> p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_out_parameter_is_collection_of_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    void M(<annotate/> out IEnumerable<int?> [|p|]) { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_out_parameter_is_collection_of_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
                .InDefaultClass(@"
                    void M(out IList<int> p) { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .Using(typeof(CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    void M([CompilerGenerated] IEnumerable<string> p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_base_class_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class B
                    {
                        public virtual void M([ItemNotNull] IEnumerable<string> p) { }
                    }

                    class D1 : B { }

                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override void M(IEnumerable<string> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_indexer_parameter_in_base_class_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    abstract class B
                    {
                        public abstract int this[[ItemNotNull] IEnumerable<string> p] { get; set; }
                    }

                    abstract class D1 : B { }

                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override int this[IEnumerable<string> p]
                        {
                            get { throw new NotImplementedException(); }
                            set { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_base_constructor_is_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class B
                    {
                        protected B([ItemNotNull] IEnumerable<string> p) { }
                    }

                    class D : B
                    {
                        public D(<annotate/> IEnumerable<string> [|p|]) : base(p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_in_implicit_interface_implementation_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    interface I
                    {
                        void M([ItemCanBeNull] IEnumerable<string> p);
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        public void M(IEnumerable<string> p) { }

                        // unrelated overload
                        public void M([ItemCanBeNull] IEnumerable<object> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_parameter_in_explicit_interface_implementation_is_effectively_annotated_through_annotation_on_interface_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    interface I
                    {
                        void M([ItemCanBeNull] IEnumerable<string> p);
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(IEnumerable<string> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_implicit_interface_implementation_is_not_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    interface I
                    {
                        void M([ItemNotNull] IEnumerable<string> p);
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(IEnumerable<string> p) { }

                        public void M(<annotate/> IEnumerable<string> [|p|]) { }

                        // unrelated overload
                        public void M([ItemNotNull] IEnumerable<object> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void
            When_parameter_in_implicit_interface_implementation_is_effectively_annotated_through_annotation_on_interface_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    interface I
                    {
                        void M([ItemNotNull] IEnumerable<string> p);
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(IEnumerable<string> p) { }

                        // requires explicit decoration
                        public void M([ItemNotNull] IEnumerable<string> p) { }

                        // unrelated overload
                        public void M([ItemNotNull] IEnumerable<object> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_is_in_file_with_codegen_comment_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .WithHeader(@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
")
                .InDefaultClass(@"
                    void M(IEnumerable<string> p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_type_is_lazy_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    void M(<annotate/> Lazy<string> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_type_is_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task).Namespace)
                .InDefaultClass(@"
                    public void M(Task p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_type_is_generic_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task<>).Namespace)
                .InDefaultClass(@"
                    void M(<annotate/> Task<string> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_type_is_generic_value_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithReference(typeof(ValueTask<>).Assembly)
                .Using(typeof(ValueTask<>).Namespace)
                .InDefaultClass(@"
                    void M(<annotate/> ValueTask<string> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_base_parameter_inherits_item_annotation_from_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(List<>).Namespace)
                .InGlobalScope(@"
                    namespace N
                    {
                        public interface I
                        {
                            void M([ItemNotNull] List<string> p);
                        }

                        public class B : I
                        {
                            public virtual void M(List<string> p) { }
                        }

                        public class C : B
                        {
                            public override void M(List<string> p) { }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_override_breaks_inheritance_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    namespace N
                    {
                        public class B
                        {
                            public virtual void M([ItemNotNull] IEnumerable<int?> p) { throw new NotImplementedException(); }
                        }

                        public class C : B
                        {
                            public new void M(<annotate/> IEnumerable<int?> [|p|]) { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }
    }
}
