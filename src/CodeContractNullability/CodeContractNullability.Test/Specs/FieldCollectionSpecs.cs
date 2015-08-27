using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting item nullability diagnostics on fields of collection types.
    /// </summary>
    [TestFixture]
    internal class FieldCollectionSpecs : ItemNullabilityNUnitRoslynTest
    {
        [Test]
        public void When_field_is_annotated_with_item_not_nullable_it_must_be_skipped()
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
                        [N.M.ItemNotNull] // Using fully qualified namespace
                        System.Collections.Generic.IEnumerable<string> f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_annotated_with_item_nullable_it_must_be_skipped()
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
                        using ICBN = N1.ItemCanBeNullAttribute;

                        class C
                        {
                            [ICBN()] // Using type/namespace alias
                            System.Collections.Generic.IEnumerable<string> f;
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_is_constant_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    public const string[] f = null;
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_item_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    System.Collections.Generic.List<int> f;
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_item_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        System.Collections.Generic.IList<T> f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_item_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .WithReference(typeof (HashSet<>).Assembly)
                .Using("System.Reflection")
                .InDefaultClass(@"
                    System.Collections.Generic.HashSet<BindingFlags> f;
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_item_type_is_object_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .InGlobalNamespace())
                .InDefaultClass(@"
                    [NotNull]
                    <annotate/> System.Collections.IList [|f|];
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_field_item_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .InGlobalNamespace())
                .InDefaultClass(@"
                    <annotate/> System.Collections.Generic.IList<int?> [|f|];
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_field_item_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        <annotate/> System.Collections.Generic.ISet<T?> [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_field_item_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.Generic.IEnumerable<string> [|f|];
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_field_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (CompilerGeneratedAttribute).Namespace)
                .Using(typeof (DebuggerNonUserCodeAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    System.Collections.Generic.IEnumerable<string> f;
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
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
                .Using(typeof (Component).Namespace)
                .InGlobalScope(@"
                    public partial class DerivedControl : System.Windows.Forms.Control
                    {
                        private System.Collections.Generic.IList<System.Windows.Forms.Control> controls;
                    }
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
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
                    private System.Collections.IList f;
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_contains_multiple_variables_they_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> System.Collections.Generic.List<int?> [|f|], [|g|];
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_field_type_is_lazy_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .InDefaultClass(@"
                    <annotate/> Lazy<string> [|f|];
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }

        [Test]
        public void When_field_type_is_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (Task).Namespace)
                .InDefaultClass(@"
                    Task f;
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityDiagnostic(source);
        }

        [Test]
        public void When_field_type_is_generic_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder())
                .Using(typeof (Task<>).Namespace)
                .InDefaultClass(@"
                    <annotate/> Task<string> [|f|];
                ")
                .Build();

            // Act and assert
            VerifyItemNullabilityFix(source);
        }
    }
}