using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting item nullability diagnostics on fields of collection types.
    /// </summary>
    public sealed class FieldCollectionSpecs : ItemNullabilityNUnitRoslynTest
    {
        [Fact]
        public void When_field_is_annotated_with_item_not_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .InCodeNamespace("N.M"))
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [N.M.ItemNotNull] // Using fully qualified namespace
                        IEnumerable<string> f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_is_annotated_with_item_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .InCodeNamespace("N1"))
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    namespace N2
                    {
                        using ICBN = N1.ItemCanBeNullAttribute;

                        class C
                        {
                            [ICBN()] // Using type/namespace alias
                            IEnumerable<string> f;
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_is_constant_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    public const string[] f = null;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_item_type_is_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(List<>).Namespace)
                .InDefaultClass(@"
                    List<int> f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_item_type_is_generic_value_type_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        IList<T> f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_item_type_is_enum_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithReference(typeof(HashSet<>).Assembly)
                .Using(typeof(HashSet<>).Namespace)
                .Using(typeof(BindingFlags).Namespace)
                .InDefaultClass(@"
                    HashSet<BindingFlags> f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_item_type_is_object_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IList).Namespace)
                .InDefaultClass(@"
                    <annotate/> IList [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Fact]
        public void When_field_item_type_is_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IList<>).Namespace)
                .InDefaultClass(@"
                    <annotate/> IList<int?> [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Fact]
        public void When_field_item_type_is_nullable_but_analysis_is_disabled_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .DisableReportOnNullableValueTypes)
                .Using(typeof(IList<>).Namespace)
                .InDefaultClass(@"
                    IList<int?> f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_item_type_is_generic_nullable_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Using(typeof(ISet<>).Namespace)
                .InGlobalScope(@"
                    class C<T> where T : struct
                    {
                        <annotate/> ISet<T?> [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Fact]
        public void When_field_item_type_is_reference_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InDefaultClass(@"
                    <annotate/> IEnumerable<string> [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Fact]
        public void When_field_is_compiler_generated_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .Using(typeof(CompilerGeneratedAttribute).Namespace)
                .InDefaultClass(@"
                    [CompilerGenerated]
                    IEnumerable<string> f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_is_in_designer_generated_file_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .Named("MainForm.Designer.cs")
                .WithReference(typeof(Control).Assembly)
                .Using(typeof(IList<>).Namespace)
                .Using(typeof(Control).Namespace)
                .InGlobalScope(@"
                    public partial class DerivedControl : Control
                    {
                        private IList<Control> controls;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_is_in_file_with_codegen_comment_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(IList).Namespace)
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
                    private IList f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_contains_multiple_variables_they_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(List<>).Namespace)
                .InDefaultClass(@"
                    <annotate/> List<int?> [|f|], [|g|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Fact]
        public void When_field_type_is_lazy_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    <annotate/> Lazy<string> [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Fact]
        public void When_field_type_is_task_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task).Namespace)
                .InDefaultClass(@"
                    Task f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_field_type_is_generic_task_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .Using(typeof(Task<>).Namespace)
                .InDefaultClass(@"
                    <annotate/> Task<string> [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }
    }
}
