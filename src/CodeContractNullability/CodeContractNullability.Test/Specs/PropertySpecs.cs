using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on properties.
    /// </summary>
    [TestFixture]
    internal class PropertySpecs : NullabilityNUnitRoslynTest
    {
        [Test]
        public void When_property_is_annotated_with_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
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

        [Test]
        public void When_property_is_annotated_with_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
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

        [Test]
        public void When_property_is_externally_annotated_with_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
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

        [Test]
        public void When_property_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    int P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_property_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
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

        [Test]
        public void When_property_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (BindingFlags).Namespace)
                .InDefaultClass(@"
                    BindingFlags P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_property_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> int? [|P|] { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_property_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        <annotate/> T? [|P|] { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_property_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> string [|P|] { get; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_property_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    string P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_property_is_not_debuggable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (DebuggerNonUserCodeAttribute).Namespace)
                .InDefaultClass(@"
                    [DebuggerNonUserCode]
                    string P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_type_of_index_property_is_reference_type_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> int? [|this|][int p]
                    {
                        get { throw new NotImplementedException(); }
                        set { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_property_in_base_class_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
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

        [Test]
        public void When_property_in_base_class_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
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

        [Test]
        public void When_property_in_implicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
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

        [Test]
        public void When_property_in_implicit_interface_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
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

        [Test]
        public void When_property_in_explicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
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

        [Test]
        public void When_property_in_explicit_interface_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
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

        [Test]
        public void
            When_property_in_implicit_interface_is_not_annotated_with_explicit_interface_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        [NotNull]
                        string P { get; set; }
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        string I.P { get; set; }

                        <annotate/> public string [|P|] { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void
            When_return_value_of_indexer_property_in_implicit_interface_is_not_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
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

                            <annotate/> public int? [|this|][char p]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void
            When_property_in_implicit_interface_is_annotated_with_externally_annotated_explicit_interface_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
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

        [Test]
        public void When_property_is_in_file_with_codegen_comment_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
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

        [Test]
        public void When_containing_type_is_decorated_with_conditional_its_members_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .WithReference(typeof (ConditionalAttribute).Assembly)
                .Using(typeof (ConditionalAttribute).Namespace)
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
    }
}