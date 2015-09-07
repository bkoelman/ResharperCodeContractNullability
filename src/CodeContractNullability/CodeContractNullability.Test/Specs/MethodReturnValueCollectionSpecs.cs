using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
                .Using(typeof (IList<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [ItemNotNull]
                        IList<string> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IList<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [ItemCanBeNull]
                        IList<string> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    IEnumerable<int> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        IEnumerable<T> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IEnumerable<>).Namespace)
                .Using(typeof (BindingFlags).Namespace)
                .InDefaultClass(@"
                    IEnumerable<BindingFlags> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IList<>).Namespace)
                .InDefaultClass(@"
                    <annotate/> IList<int?> [|M|]() { throw new NotImplementedException(); }
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
                .Using(typeof (List<>).Namespace)
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        <annotate/> List<T?> [|M|]() { throw new NotImplementedException(); }
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
                .Using(typeof (IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    <annotate/> IEnumerable<string> [|M|]() { throw new NotImplementedException(); }
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
                .Using(typeof (IEnumerable).Namespace)
                .InDefaultClass(@"
                    <annotate/> IEnumerable [|M|]() { throw new NotImplementedException(); }
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
                .Using(typeof (IEnumerable<>).Namespace)
                .Using(typeof (CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    IEnumerable<string> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IEnumerable<>).Namespace)
                .Using(typeof (DebuggerNonUserCodeAttribute).Namespace)
                .InDefaultClass(@"
                    [DebuggerNonUserCode]
                    IEnumerable<string> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IList<>).Namespace)
                .InGlobalScope(@"
                    class B
                    {
                        [ItemNotNull]
                        public virtual IList<string> M() { throw new NotImplementedException(); }
                    }

                    class D1 : B { }

                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override IList<string> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IList<>).Namespace)
                .InGlobalScope(@"
                    interface I
                    {
                        [ItemCanBeNull]
                        IList<string> M();
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        public IList<string> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IList<>).Namespace)
                .InGlobalScope(@"
                    interface I
                    {
                        [ItemCanBeNull]
                        IList<string> M();
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        IList<string> I.M() { throw new NotImplementedException(); }
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
                .Using(typeof (IList<>).Namespace)
                .InGlobalScope(@"
                    interface I
                    {
                        [ItemNotNull]
                        IList<string> M();
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        IList<string> I.M() { throw new NotImplementedException(); }

                        <annotate/> public IList<string> [|M|]() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_return_value_in_implicit_interface_is_annotated_with_explicit_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof (IList<>).Namespace)
                .InGlobalScope(@"
                    interface I
                    {
                        [ItemNotNull]
                        IList<string> M();
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        IList<string> I.M() { throw new NotImplementedException(); }

                        // requires explicit decoration
                        [ItemNotNull]
                        public IList<string> M() { throw new NotImplementedException(); }
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
                .Using(typeof (IList<>).Namespace)
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
                    IList<string> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_type_is_lazy_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    <annotate/> Lazy<string> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_return_value_type_is_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof (Task).Namespace)
                .InDefaultClass(@"
                    Task M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_return_value_type_is_generic_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof (Task<>).Namespace)
                .InDefaultClass(@"
                    <annotate/> Task<string> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_base_method_inherits_item_annotation_from_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof (IList<>).Namespace)
                .InGlobalScope(@"
                    namespace N
                    {
                        public interface I
                        {
                            [ItemNotNull]
                            IList<string> M();
                        }

                        public class B : I
                        {
                            public virtual IList<string> M() { throw new NotImplementedException(); }
                        }

                        public class C : B
                        {
                            public override IList<string> M() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_override_breaks_inheritance_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof (IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    namespace N
                    {
                        public class B
                        {
                            [ItemNotNull]
                            public virtual IEnumerable<int?> M() { throw new NotImplementedException(); }
                        }

                        public class C : B
                        {
                            <annotate/> public new IEnumerable<int?> [|M|]() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }
    }
}