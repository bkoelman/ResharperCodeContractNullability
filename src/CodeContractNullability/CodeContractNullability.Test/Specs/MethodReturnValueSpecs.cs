using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on methods (meaning: method return values).
    /// </summary>
    public sealed class MethodReturnValueSpecs : NullabilityTest
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
        public void When_return_value_is_annotated_with_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        [NotNull]
                        string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_is_annotated_with_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        [CanBeNull]
                        string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        class C
                        {
                            string M() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.C.M")
                        .NotNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    int M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        T M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(BindingFlags).Namespace)
                .InDefaultClass(@"
                    BindingFlags M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    int? [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_return_value_type_is_nullable_but_analysis_is_disabled_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .DisableReportOnNullableValueTypes)
                .InDefaultClass(@"
                    int? M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        [+NullabilityAttributePlaceholder+]
                        T? [|M|]() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_return_value_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    private protected string [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_method_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    string M() { throw new NotImplementedException(); }
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
                .Using(typeof(DebuggerNonUserCodeAttribute).Namespace)
                .InDefaultClass(@"
                    [DebuggerNonUserCode]
                    string M() { throw new NotImplementedException(); }
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
                .Using(typeof(Task).Namespace)
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
        public void When_async_method_returns_generic_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task<>).Namespace)
                .InDefaultClass(@"
                    async Task<string> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_method_return_value_is_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task).Namespace)
                .InDefaultClass(@"
                    [+NullabilityAttributePlaceholder+]
                    Task [|M|]() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
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
        public void When_method_return_value_is_generic_value_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithReference(typeof(ValueTask<>).Assembly)
                .Using(typeof(ValueTask<>).Namespace)
                .InDefaultClass(@"
                    ValueTask<string> M() { throw new NotImplementedException(); }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_method_is_lambda_named_by_compiler_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
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
                        public static void M([NotNull] Func<int?> callback)
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
                        public static void M([NotNull] Func<int?> callback)
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
                .InGlobalScope(@"
                    class B
                    {
                        [NotNull]
                        public virtual string M() { throw new NotImplementedException(); }
                    }

                    class D1 : B { }

                    class D2 : D1
                    {
                        // implicitly inherits decoration from base class
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_in_base_class_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        class B
                        {
                            public virtual string M() { throw new NotImplementedException(); }
                        }

                        class D1 : B { }

                        class D2 : D1
                        {
                            // implicitly inherits decoration from base class
                            public override string M() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.B.M")
                        .NotNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_in_implicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    interface I
                    {
                        [CanBeNull]
                        string M();
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        public string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_in_implicit_interface_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            string M();
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            public string M() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.I.M")
                        .CanBeNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_in_explicit_interface_is_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    interface I
                    {
                        [CanBeNull]
                        string M();
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        string I.M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_in_explicit_interface_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            string M();
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            string I.M() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.I.M")
                        .CanBeNull()))
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
                .InGlobalScope(@"
                    interface I
                    {
                        [NotNull]
                        string M();
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        string I.M() { throw new NotImplementedException(); }

                        [+NullabilityAttributePlaceholder+]
                        public string [|M|]() { throw new NotImplementedException(); }
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
                .InGlobalScope(@"
                    interface I
                    {
                        [NotNull]
                        string M();
                    }

                    class C : I
                    {
                        // implicitly inherits decoration from interface
                        string I.M() { throw new NotImplementedException(); }

                        // requires explicit decoration
                        [NotNull]
                        public string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void
            When_return_value_in_implicit_interface_is_annotated_with_externally_annotated_explicit_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        interface I
                        {
                            string M();
                        }

                        class C : I
                        {
                            // implicitly inherits decoration from interface
                            string I.M() { throw new NotImplementedException(); }

                            // requires explicit decoration
                            [NotNull]
                            public string M() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:N.I.M")
                        .CanBeNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_return_value_is_in_file_with_codegen_comment_at_top_it_must_be_skipped()
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
                    string M() { throw new NotImplementedException(); }
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
                            string M() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_base_method_inherits_annotation_from_interface_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N
                    {
                        public interface I
                        {
                            [NotNull]
                            string M();
                        }

                        public class B : I
                        {
                            public virtual string M() { throw new NotImplementedException(); }
                        }

                        public class C : B
                        {
                            public override string M() { throw new NotImplementedException(); }
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
                            public virtual string M() { throw new NotImplementedException(); }
                        }

                        public class C : B
                        {
                            [+NullabilityAttributePlaceholder+]
                            public new string [|M|]() { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }
    }
}
