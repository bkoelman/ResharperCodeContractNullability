using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on properties.
    /// </summary>
    public sealed class PropertySpecs : NullabilityTest
    {
        [Fact]
        public void When_property_is_annotated_with_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        [NotNull]
                        string P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_is_annotated_with_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        [CanBeNull]
                        string P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        class C
                        {
                            string P { get; set; }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:N.C.P")
                        .NotNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    int P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        T P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(BindingFlags).Namespace)
                .InDefaultClass(@"
                    BindingFlags P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

#if !NET452
        [Fact]
        public void When_property_type_is_generic_enum_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : Enum
                    {
                        [+NullabilityAttributePlaceholder+]
                        T [|P|] { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_type_is_generic_unmanaged_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : unmanaged
                    {
                        T P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_type_is_generic_delegate_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : Delegate
                    {
                        [+NullabilityAttributePlaceholder+]
                        T [|P|] { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_type_is_generic_multicast_delegate_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : MulticastDelegate
                    {
                        [+NullabilityAttributePlaceholder+]
                        private protected T [|P|]
                        {
                            get => throw new NotImplementedException();
                            set => throw new NotImplementedException();
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }
#endif

        [Fact]
        public void When_property_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    int? [|P|] { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_type_is_nullable_but_analysis_is_disabled_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .DisableReportOnNullableValueTypes)
                .InDefaultClass(@"
                    int? P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        [+NullabilityAttributePlaceholder+]
                        T? [|P|] { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    string [|P|] { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    string P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_is_not_debuggable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(DebuggerNonUserCodeAttribute).Namespace)
                .InDefaultClass(@"
                    [DebuggerNonUserCode]
                    string P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_indexer_property_type_is_reference_type_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    int? [|this|][int p]
                    {
                        get { throw new NotImplementedException(); }
                        set { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("this[]"));
        }

        [Fact]
        public void When_property_in_base_class_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class B
                    {
                        [NotNull]
                        public virtual string P { get; set; }
                    }

                    class D1 : B { }

                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override string P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_base_class_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        class B
                        {
                            public virtual string P { get; set; }
                        }

                        class D1 : B { }

                        class D2 : D1
                        {
                            // implicitly inherits decoration from base class
                            public override string P { get; set; }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:N.B.P")
                        .NotNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_implicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    interface I
                    {
                        [CanBeNull]
                        string P { get; set; }
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        public string P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_implicit_interface_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            string P { get; set; }
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            public string P { get; set; }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:N.I.P")
                        .CanBeNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_explicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    interface I
                    {
                        [CanBeNull]
                        string P { get; set; }
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        string I.P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_explicit_interface_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            string P { get; set; }
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            string I.P { get; set; }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:N.I.P")
                        .CanBeNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_implicit_interface_is_not_annotated_with_explicit_interface_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    interface I
                    {
                        [NotNull]
                        string P { get; set; }
                    }

                    class C : I
                    {
                        [+NullabilityAttributePlaceholder+]
                        public string [|P|] { get; set; }

                        // implicitly inherits decoration from interface
                        string I.P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_indexer_property_type_in_implicit_interface_is_not_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            [CanBeNull]
                            int? this[char p] { get; set; }
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            int? I.this[char p]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }

                            [+NullabilityAttributePlaceholder+]
                            public int? [|this|][char p]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("this[]"));
        }

        [Fact]
        public void
            When_property_in_implicit_interface_is_annotated_with_externally_annotated_explicit_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            string P { get; set; }
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            string I.P { get; set; }

                            // requires explicit decoration
                            [NotNull]
                            public string P { get; set; }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:N.I.P")
                        .NotNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_is_in_file_with_code_gen_comment_at_top_it_must_be_skipped()
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
                    string this[int index]
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                        set
                        {
                            throw new NotImplementedException();
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_containing_type_is_decorated_with_conditional_its_members_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithReference(typeof(ConditionalAttribute).Assembly)
                .Using(typeof(ConditionalAttribute).Namespace)
                .InGlobalScope(@"
                    namespace N
                    {
                        [Conditional(""JETBRAINS_ANNOTATIONS"")]
                        class C : Attribute
                        {
                            public string P { get; set; }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_base_property_inherits_annotation_from_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        public interface I
                        {
                            [NotNull]
                            string P { get; set; }
                        }

                        public class B : I
                        {
                            public virtual string P { get; set; }
                        }

                        public class C : B
                        {
                            public override string P { get; set; }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_override_breaks_inheritance_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        public class B
                        {
                            [NotNull]
                            public virtual string P { get; set; }
                        }

                        public class C : B
                        {
                            [+NullabilityAttributePlaceholder+]
                            public new string [|P|] { get; set; }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }
    }
}
