﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting item nullability diagnostics on methods (meaning: method return values).
    /// </summary>
    public sealed class MethodReturnValueCollectionSpecs : ItemNullabilityTest
    {
        [Fact]
        public void When_method_returns_void_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    void M() { }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_is_annotated_with_item_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [ItemNotNull]
                        IList<string> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_is_annotated_with_item_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [ItemCanBeNull]
                        IList<string> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_collection_of_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    IEnumerable<int> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_collection_of_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        IEnumerable<T> M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_collection_of_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .Using(typeof(BindingFlags).Namespace)
                .InDefaultClass(@"
                    IEnumerable<BindingFlags> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_collection_of_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    IList<int?> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_return_value_type_is_collection_of_nullable_but_analysis_is_disabled_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .DisableReportOnNullableValueTypes)
                .Using(typeof(IList<>).Namespace)
                .InDefaultClass(@"
                    IList<int?> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_collection_of_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(List<>).Namespace)
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        [+NullabilityAttributePlaceholder+]
                        List<T?> [|M|]() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_return_value_type_is_collection_of_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    IEnumerable<string> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_return_value_type_is_collection_of_object_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable).Namespace)
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    IEnumerable [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

#if !NET452
        [Fact]
        public void When_return_value_type_is_tuple_of_reference_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(ValueTuple<>).Namespace)
                .InDefaultClass(@"
                    private protected (string, string) M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }
#endif

        [Fact]
        public void When_method_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .Using(typeof(CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    IEnumerable<string> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_method_is_not_debuggable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .Using(typeof(DebuggerNonUserCodeAttribute).Namespace)
                .InDefaultClass(@"
                    [DebuggerNonUserCode]
                    IEnumerable<string> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_async_method_returns_void_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    async void M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_async_method_returns_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task).Namespace)
                .InDefaultClass(@"
                    async Task M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_async_method_returns_generic_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task<>).Namespace)
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    async Task<string> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_method_return_value_is_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task).Namespace)
                .InDefaultClass(@"
                    Task M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_method_return_value_is_generic_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task<>).Namespace)
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    Task<string> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_method_return_value_is_generic_value_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithReference(typeof(ValueTask<>).Assembly)
                .Using(typeof(ValueTask<>).Namespace)
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    ValueTask<string> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_method_is_lambda_named_by_compiler_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
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
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_method_is_anonymous_named_by_compiler_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
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
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_in_base_class_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
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
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_in_implicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
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
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_in_explicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
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
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_return_value_in_implicit_interface_is_not_annotated_with_explicit_interface_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
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

                        [+NullabilityAttributePlaceholder+]
                        public IList<string> [|M|]() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_return_value_in_implicit_interface_is_annotated_with_explicit_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
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
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_is_in_file_with_code_gen_comment_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
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
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_lazy_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    Lazy<string> [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_return_value_type_is_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task).Namespace)
                .InDefaultClass(@"
                    Task M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_base_method_inherits_item_annotation_from_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
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
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_override_breaks_inheritance_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
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
                            [+NullabilityAttributePlaceholder+]
                            public new IEnumerable<int?> [|M|]() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }
    }
}
