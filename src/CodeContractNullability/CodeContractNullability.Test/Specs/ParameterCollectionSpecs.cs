using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting item nullability diagnostics on method parameters.
    /// </summary>
    [TestFixture]
    internal class ParameterCollectionSpecs : ItemNullabilityNUnitRoslynTest
    {
        [Test]
        public void When_parameter_is_annotated_with_item_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class C
                    {
                        void M([ItemNotNull] System.Collections.Generic.IEnumerable<string> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_is_annotated_with_item_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class C
                    {
                        void M([ItemCanBeNull] System.Collections.Generic.IEnumerable<string> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_item_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    public void M(System.Collections.Generic.List<int> someValue) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_item_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        void M(System.Collections.Generic.IEnumerable<T> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_item_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (BindingFlags).Namespace)
                .InDefaultClass(@"
                    void M(System.Collections.Generic.IEnumerable<BindingFlags> p) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_item_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    void M(<annotate/> System.Collections.Generic.IEnumerable<int?> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_parameter_item_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        void M(<annotate/> System.Collections.Generic.IEnumerable<T?> [|p|]) { }
                    }
                    ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_parameter_item_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    void M(<annotate/> System.Collections.Generic.IEnumerable<string> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_parameter_item_type_is_object_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    void M(<annotate/> System.Collections.IEnumerable [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_indexer_parameter_is_collection_of_reference_type_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    int this[<annotate/> System.Collections.Generic.IEnumerable<string> [|p|]]
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
        public void When_ref_parameter_is_collection_of_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    void M(<annotate/> ref System.Collections.Generic.IEnumerable<int?> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_ref_parameter_is_collection_of_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    void M(ref System.Collections.Generic.IEnumerable<int> p) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_out_parameter_is_collection_of_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    void M(<annotate/> out System.Collections.Generic.IEnumerable<int?> [|p|]) { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_out_parameter_is_collection_of_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    void M(out System.Collections.Generic.IList<int> p) { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    void M([CompilerGenerated] System.Collections.Generic.IEnumerable<string> p) { }                        
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_in_base_class_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class B
                    {
                        public virtual void M([ItemNotNull] System.Collections.Generic.IEnumerable<string> p) { }
                    }
                        
                    class D1 : B { }
                        
                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override void M(System.Collections.Generic.IEnumerable<string> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_indexer_parameter_in_base_class_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    abstract class B
                    {
                        public abstract int this[[ItemNotNull] System.Collections.Generic.IEnumerable<string> p] { get; set; }
                    }
                        
                    abstract class D1 : B { }
                        
                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override int this[System.Collections.Generic.IEnumerable<string> p]
                        {
                            get { throw new NotImplementedException(); }
                            set { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_in_base_constructor_is_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class B
                    {
                        protected B([ItemNotNull] System.Collections.Generic.IEnumerable<string> p) { }
                    }
                        
                    class D : B
                    {
                        public D(<annotate/> System.Collections.Generic.IEnumerable<string> [|p|]) : base(p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_parameter_in_implicit_interface_implementation_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        void M([ItemCanBeNull] System.Collections.Generic.IEnumerable<string> p);
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        public void M(System.Collections.Generic.IEnumerable<string> p) { }
                            
                        // unrelated overload
                        public void M([ItemCanBeNull] System.Collections.Generic.IEnumerable<object> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void
            When_parameter_in_explicit_interface_implementation_is_effectively_annotated_through_annotation_on_interface_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        void M([ItemCanBeNull] System.Collections.Generic.IEnumerable<string> p);
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(System.Collections.Generic.IEnumerable<string> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_in_implicit_interface_implementation_is_not_annotated_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        void M([ItemNotNull] System.Collections.Generic.IEnumerable<string> p);
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(System.Collections.Generic.IEnumerable<string> p) { }

                        public void M(<annotate/> System.Collections.Generic.IEnumerable<string> [|p|]) { }

                        // unrelated overload
                        public void M([ItemNotNull] System.Collections.Generic.IEnumerable<object> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void
            When_parameter_in_implicit_interface_implementation_is_effectively_annotated_through_annotation_on_interface_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        void M([ItemNotNull] System.Collections.Generic.IEnumerable<string> p);
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        void I.M(System.Collections.Generic.IEnumerable<string> p) { }

                        // requires explicit decoration
                        public void M([ItemNotNull] System.Collections.Generic.IEnumerable<string> p) { }
                        
                        // unrelated overload
                        public void M([ItemNotNull] System.Collections.Generic.IEnumerable<object> p) { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_is_in_file_with_codegen_comment_at_top_it_must_be_skipped()
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
                    void M(System.Collections.Generic.IEnumerable<string> p) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_type_is_lazy_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    void M(<annotate/> Lazy<string> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_parameter_type_is_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (Task).Namespace)
                .InDefaultClass(@"
                    public void M(Task p) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_parameter_type_is_generic_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (Task<>).Namespace)
                .InDefaultClass(@"
                    void M(<annotate/> Task<string> [|p|]) { }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }
    }
}