using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting item nullability diagnostics on methods (meaning: method return values).
    /// </summary>
    [TestFixture]
    internal class MethodReturnValueCollectionSpecs : ItemNullabilityNUnitRoslynTest
    {
        [Test]
        public void When_return_value_is_annotated_with_item_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class C
                    {
                        [ItemNotNull]
                        System.Collections.Generic.IList<string> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_is_annotated_with_item_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class C
                    {
                        [ItemCanBeNull]
                        System.Collections.Generic.IList<string> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_type_is_collection_of_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    System.Collections.Generic.IEnumerable<int> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_type_is_collection_of_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        System.Collections.Generic.IEnumerable<T> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_type_is_collection_of_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (BindingFlags).Namespace)
                .InDefaultClass(@"
                    System.Collections.Generic.IEnumerable<BindingFlags> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_type_is_collection_of_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.Generic.IList<int?> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_return_value_type_is_collection_of_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        <annotate/> System.Collections.Generic.List<T?> [|M|]() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_return_value_type_is_collection_of_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.Generic.IEnumerable<string> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_return_value_type_is_collection_of_object_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.IEnumerable [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_method_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    System.Collections.Generic.IEnumerable<string> M() { throw new NotImplementedException(); }                        
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_method_is_not_debuggable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (DebuggerNonUserCodeAttribute).Namespace)
                .InDefaultClass(@"
                    [DebuggerNonUserCode]
                    System.Collections.Generic.IEnumerable<string> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_method_is_lambda_named_by_compiler_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .Using(typeof (IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C1
                    {
                        private void Test()
                        {
                            C2.M( () =>     // no syntax exists to decorate this lambda expression
                            {
                                throw new NotImplementedException();
                            });
                        }
                    }
                    public class C2
                    {
                        public static void M([ItemNotNull] Func<IEnumerable<int?>> callback)
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_method_is_anonymous_named_by_compiler_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .Using(typeof (IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C1
                    {
                        private void Test()
                        {
                            C2.M(delegate       // no syntax exists to decorate this anonymous method
                            {
                                throw new NotImplementedException();
                            });
                        }
                    }
                    public class C2
                    {
                        public static void M([ItemNotNull] Func<IEnumerable<int?>> callback)
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_in_base_class_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    class B
                    {
                        [ItemNotNull]
                        public virtual System.Collections.Generic.IList<string> M() { throw new NotImplementedException(); }
                    }
                        
                    class D1 : B { }
                        
                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override System.Collections.Generic.IList<string> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_in_implicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        [ItemCanBeNull]
                        System.Collections.Generic.IList<string> M();
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        public System.Collections.Generic.IList<string> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_in_explicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        [ItemCanBeNull]
                        System.Collections.Generic.IList<string> M();
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        System.Collections.Generic.IList<string> I.M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void
            When_return_value_in_implicit_interface_is_not_annotated_with_explicit_interface_it_must_be_reported_and_fixed
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        [ItemNotNull]
                        System.Collections.Generic.IList<string> M();
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        System.Collections.Generic.IList<string> I.M() { throw new NotImplementedException(); }

                        public System.Collections.Generic.IList<string> [|M|]() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_in_implicit_interface_is_annotated_with_explicit_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .Imported())
                .InGlobalScope(@"
                    interface I
                    {
                        [ItemNotNull]
                        System.Collections.Generic.IList<string> M();
                    }
                        
                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        System.Collections.Generic.IList<string> I.M() { throw new NotImplementedException(); }

                        // requires explicit decoration
                        [ItemNotNull]
                        public System.Collections.Generic.IList<string> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_is_in_file_with_codegen_comment_at_top_it_must_be_skipped()
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
                    System.Collections.Generic.IList<string> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }
    }
}