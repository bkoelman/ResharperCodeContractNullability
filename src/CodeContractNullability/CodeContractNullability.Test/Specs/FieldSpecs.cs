using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on fields.
    /// </summary>
    [TestFixture]
    internal class FieldSpecs : NullabilityNUnitRoslynTest
    {
        [Test]
        public void When_field_is_annotated_with_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N.M
                    {
                        internal class NotNullAttribute : Attribute { }
                        internal class CanBeNullAttribute : Attribute { }
                        internal class ItemNotNullAttribute : Attribute { }
                        internal class ItemCanBeNullAttribute : Attribute { }
                    }

                    class C
                    {
                        [N.M.NotNull] // Using fully qualified namespace
                        string f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_annotated_with_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    namespace N1
                    {
                        internal class NotNullAttribute : Attribute { }
                        internal class CanBeNullAttribute : Attribute { }
                        internal class ItemNotNullAttribute : Attribute { }
                        internal class ItemCanBeNullAttribute : Attribute { }
                    }

                    namespace N2
                    {
                        using CBN = N1.CanBeNullAttribute;

                        class C
                        {
                            [CBN()] // Using type/namespace alias
                            string f;
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    namespace N
                    {
                        public class C
                        {
                            public string F;
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("F:N.C.F")
                        .NotNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_constant_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    public const string F = ""X"";
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    int f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        T f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (BindingFlags).Namespace)
                .InDefaultClass(@"
                    BindingFlags f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> int? [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_field_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        <annotate/> T? [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_generic_field_is_externally_annotated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    namespace N
                    {
                        public class C<T> where T : struct
                        {
                            public T? F;
                        }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("F:N.C`1.F")
                        .CanBeNull()))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> string [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_field_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    string f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_event_handler_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (EventHandler).Namespace)
                .InDefaultClass(@"
                    public event EventHandler e;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_custom_event_handler_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (EventHandler<>).Namespace)
                .Using(typeof (EventArgs).Namespace)
                .InGlobalScope(@"
                    public class DerivedEventArgs : EventArgs { }

                    class C
                    {
                        public event EventHandler<DerivedEventArgs> e;
                    }

                    class D<T> where T : EventArgs
                    {
                        public event EventHandler<T> e;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_in_designer_generated_file_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Named("MainForm.Designer.cs")
                .WithReference(typeof (Control).Assembly)
                .Using(typeof (Control).Namespace)
                .Using(typeof (Button).Namespace)
                .InGlobalScope(@"
                    public partial class DerivedControl : Control
                    {
                        private Button button1;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_in_file_with_codegen_comment_at_top_it_must_be_skipped()
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
                    private string f;
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
                            private string f;
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_contains_multiple_variables_they_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> int? [|f|], [|g|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }
    }
}