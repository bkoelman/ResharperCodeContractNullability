using CodeContractNullability.Settings;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs.AlternateTypeHierarchyModes
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on methods (meaning: method return values) with non-default type hierarchy
    /// configurations.
    /// </summary>
    public sealed class MethodReturnValueSpecs : NullabilityTest
    {
        [Fact]
        public void When_method_in_mode_AtHighestSourceInTypeHierarchy_with_interface_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .InGlobalScope(@"
                    public interface I
                    {
                        <annotate/> string [|M|]();
                    }

                    public abstract class B : I
                    {
                        public abstract string M();
                    }

                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void
            When_method_in_mode_AtHighestSourceInTypeHierarchy_with_external_interface_at_top_it_must_report_at_highest_in_source()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        string M();
                    }")
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        <annotate/> public abstract string [|M|]();
                    }

                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void
            When_method_in_mode_AtHighestSourceInTypeHierarchy_with_annotated_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        string M();
                    }")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:I.M")
                        .NotNull()))
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract string M();
                    }

                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_method_in_mode_AtHighestSourceInTypeHierarchy_with_base_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .InGlobalScope(@"
                    public abstract class B
                    {
                        <annotate/> public abstract string [|M|]();
                    }

                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void
            When_method_in_mode_AtHighestSourceInTypeHierarchy_with_external_base_at_top_it_must_report_at_highest_in_source()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public abstract class B
                    {
                        public abstract string M();
                    }")
                .InGlobalScope(@"
                    public class D : B
                    {
                        <annotate/> public override string [|M|]() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_method_in_mode_AtTopInTypeHierarchy_with_interface_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .InGlobalScope(@"
                    public interface I
                    {
                        <annotate/> string [|M|]();
                    }

                    public abstract class B : I
                    {
                        public abstract string M();
                    }

                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_method_in_mode_AtTopInTypeHierarchy_with_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        string M();
                    }")
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract string M();
                    }

                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_method_in_mode_AtTopInTypeHierarchy_with_annotated_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        string M();
                    }")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:I.M")
                        .NotNull()))
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract string M();
                    }

                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_method_in_mode_AtTopInTypeHierarchy_with_base_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .InGlobalScope(@"
                    public abstract class B
                    {
                        <annotate/> public abstract string [|M|]();
                    }

                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_method_in_mode_AtTopInTypeHierarchy_with_external_base_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public abstract class B
                    {
                        public abstract string M();
                    }")
                .InGlobalScope(@"
                    public class D : B
                    {
                        public override string M() { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }
    }
}
