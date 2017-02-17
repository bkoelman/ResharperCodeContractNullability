using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on method parameters.
    /// </summary>
    public sealed class ParameterSpecs : NullabilityTest
    {
        [Fact]
        public void When_parameter_is_annotated_with_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        void M([NotNull] string p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_is_annotated_with_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        void M([CanBeNull] string p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        class C
                        {
                            void M(string p) { }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.C.M(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    public void M(int p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        void M(T p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(BindingFlags).Namespace)
                .InDefaultClass(@"
                    void M(BindingFlags p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    void M(<annotate/> int? [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_type_is_nullable_but_analysis_is_disabled_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .DisableReportOnNullableValueTypes)
                .InDefaultClass(@"
                    void M(int? p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        void M(<annotate/> T? [|p|]) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    void M(<annotate/> string [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_indexer_parameter_is_reference_type_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    int this[<annotate/> string [|p|]]
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
        public void When_ref_parameter_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    void M(<annotate/> ref int? [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_ref_parameter_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    void M(ref int p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_out_parameter_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    void M(<annotate/> out int? [|p|]) { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_out_parameter_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    void M(out int p) { throw new NotImplementedException(); }
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
                .Using(typeof(CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    void M([CompilerGenerated] string p) { }
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
                .InGlobalScope(@"
                    class B
                    {
                        public virtual void M([NotNull] string p) { }
                    }

                    class D1 : B { }

                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override void M(string p) { }
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
                .InGlobalScope(@"
                    abstract class B
                    {
                        public abstract int this[[NotNull] string p] { get; set; }
                    }

                    abstract class D1 : B { }

                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override int this[string p]
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
                .InGlobalScope(@"
                    class B
                    {
                        protected B([NotNull] string p) { }
                    }

                    class D : B
                    {
                        public D(<annotate/> string [|p|]) : base(p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_in_constructor_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        public C(string p) { }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:C.#ctor(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .NotNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_base_class_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        class B
                        {
                            public virtual void M(string p) { }
                        }

                        class D1 : B { }

                        class D2 : D1
                        {
                            // implicitly inherits decoration from base class
                            public override void M(string p) { }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.B.M(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_indexer_parameter_in_base_class_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        abstract class B
                        {
                            public abstract int this[string p] { get; set; }
                        }

                        abstract class D1 : B { }

                        class D2 : D1
                        {
                            // implicitly inherits decoration from base class
                            public override int this[string p]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:N.B.Item(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_implicit_interface_implementation_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    interface I
                    {
                        void M([CanBeNull] string p);
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        public void M(string p) { }

                        // unrelated overload
                        public void M([CanBeNull] object p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_implicit_interface_implementation_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            void M(string p);
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            public void M(string p) { }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.I.M(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_indexer_parameter_in_implicit_interface_implementation_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            int this[string p] { get; set; }
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            public int this[string p]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:N.I.Item(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
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
                .InGlobalScope(@"
                    interface I
                    {
                        void M([CanBeNull] string p);
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(string p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_parameter_in_explicit_interface_implementation_is_effectively_annotated_through_external_annotation_on_interface_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            void M(string p);
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            void I.M(string p) { }

                            // unrelated overload
                            public void M(object p) { }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.I.M(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .NotNull()))
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.C.M(System.Object)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_implicit_interface_implementation_is_not_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    interface I
                    {
                        void M([NotNull] string p);
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(string p) { }

                        public void M(<annotate/> string [|p|]) { }

                        // unrelated overload
                        public void M([NotNull] object p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void
            When_indexer_parameter_in_implicit_interface_implementation_is_not_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            int this[string p] { get; set; }
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            int I.this[string p]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }

                            public int this[<annotate/> string [|p|]]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:N.I.Item(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void
            When_parameter_in_implicit_interface_implementation_is_not_externally_annotated_it_must_be_reported_and_fixed
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            void M(string p);
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            void I.M(string p) { }

                            public void M(<annotate/> string [|p|]) { }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.I.M(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
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
                .InGlobalScope(@"
                    interface I
                    {
                        void M([NotNull] string p);
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(string p) { }

                        // requires explicit decoration
                        public void M([NotNull] string p) { }

                        // unrelated overload
                        public void M([NotNull] object p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_parameter_in_implicit_interface_implementation_is_effectively_annotated_through_external_annotation_on_interface_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            void M(string p);
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            void I.M(string p) { }

                            // requires explicit decoration
                            public void M([NotNull] string p) { }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.I.M(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .CanBeNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_is_in_file_with_codegen_comment_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
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
                    void M(string p) { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_containing_type_is_decorated_with_conditional_its_members_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithReference(typeof(ConditionalAttribute).Assembly)
                .Using(typeof(ConditionalAttribute).Namespace)
                .InGlobalScope(@"
                    namespace N
                    {
                        [Conditional(""JETBRAINS_ANNOTATIONS"")]
                        class C : Attribute
                        {
                            void M(string p) { }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_base_parameter_inherits_annotation_from_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        public interface I
                        {
                            void M([NotNull] string p);
                        }

                        public class B : I
                        {
                            public virtual void M(string p) { }
                        }

                        public class C : B
                        {
                            public override void M(string p) { }
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
                .InGlobalScope(@"
                    namespace N
                    {
                        public class B
                        {
                            public virtual void M([NotNull] string p) { }
                        }

                        public class C : B
                        {
                            public new void M(<annotate/> string [|p|]) { }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }
    }
}
