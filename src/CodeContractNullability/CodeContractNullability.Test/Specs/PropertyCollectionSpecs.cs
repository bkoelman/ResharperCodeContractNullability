using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting item nullability diagnostics on properties.
    /// </summary>
    [TestFixture]
    internal class PropertyCollectionSpecs : ItemNullabilityNUnitRoslynTest
    {
        [Test]
        public void When_property_is_annotated_with_item_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class C
                    {
                        [ItemNotNull]
                        System.Collections.Generic.IEnumerable<string> P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_property_is_annotated_with_item_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class C
                    {
                        [ItemCanBeNull]
                        System.Collections.Generic.IEnumerable<string> P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_property_item_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    System.Collections.Generic.IList<int> P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_property_item_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        System.Collections.Generic.IEnumerable<T> P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_property_item_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (BindingFlags).Namespace)
                .InDefaultClass(@"
                    System.Collections.Generic.IEnumerable<BindingFlags> P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_property_item_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.Generic.IEnumerable<int?> [|P|] { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_property_item_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        <annotate/> System.Collections.Generic.IEnumerable<T?> [|P|] { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_property_item_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.Generic.IEnumerable<string> [|P|] { get; }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_property_item_type_is_object_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.ArrayList [|P|] { get; }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
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
                    System.Collections.Generic.IEnumerable<string> P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
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
                    System.Collections.Generic.IEnumerable<string> P { get; set; }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_type_of_index_property_is_collection_of_reference_type_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.Generic.IEnumerable<int?> [|this|][int p]
                    {
                        get { throw new NotImplementedException(); }
                        set { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
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
                        [ItemNotNull]
                        public virtual System.Collections.Generic.IEnumerable<string> P { get; set; }
                    }
                        
                    class D1 : B { }
                        
                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override System.Collections.Generic.IEnumerable<string> P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
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
                        [ItemCanBeNull]
                        System.Collections.Generic.IList<string> P { get; set; }
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        public System.Collections.Generic.IList<string> P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
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
                        [ItemCanBeNull]
                        System.Collections.Generic.IEnumerable<string> P { get; set; }
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        System.Collections.Generic.IEnumerable<string> I.P { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
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
                        [ItemNotNull]
                        System.Collections.Generic.IEnumerable<string> P { get; set; }
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        System.Collections.Generic.IEnumerable<string> I.P { get; set; }

                        <annotate/> public System.Collections.Generic.IEnumerable<string> [|P|] { get; set; }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void
            When_return_value_of_indexer_property_in_implicit_interface_is_not_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .Using(typeof (IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            [ItemCanBeNull]
                            IEnumerable<int?> this[char p] { get; set; }
                        }
                        
                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            IEnumerable<int?> I.this[char p]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }

                            <annotate/> public IEnumerable<int?> [|this|][char p]
                            {
                                get { throw new NotImplementedException(); }
                                set { throw new NotImplementedException(); }
                            }
                        }
                    }
                    ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
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
                    System.Collections.Generic.IEnumerable<string> this[int index] 
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
            VerifyItemNullabilityDiagnostic(source);
        }
    }
}