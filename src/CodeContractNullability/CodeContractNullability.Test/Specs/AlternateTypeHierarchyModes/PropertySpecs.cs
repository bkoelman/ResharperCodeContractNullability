using CodeContractNullability.Settings;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs.AlternateTypeHierarchyModes
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on properties with non-default type hierarchy configurations.
    /// </summary>
    public sealed class PropertySpecs : NullabilityTest
    {
        [Fact]
        public void
            When_property_in_mode_AtHighestSourceInTypeHierarchy_with_interface_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .InGlobalScope(@"
                    public interface I
                    {
                        <annotate/> string [|P|] { get; }
                    }

                    public abstract class B : I
                    {
                        public abstract string P { get; }
                    }

                    public class D : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void
            When_property_in_mode_AtHighestSourceInTypeHierarchy_with_external_interface_at_top_it_must_report_at_highest_in_source()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        string P { get; }
                    }")
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        <annotate/> public abstract string [|P|] { get; }
                    }

                    public class D : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void
            When_property_in_mode_AtHighestSourceInTypeHierarchy_with_annotated_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        string P { get; }
                    }")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:I.P")
                        .NotNull()))
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract string P { get; }
                    }

                    public class D : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_mode_AtHighestSourceInTypeHierarchy_with_base_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .InGlobalScope(@"
                    public abstract class B
                    {
                        <annotate/> public abstract string [|P|] { get; }
                    }

                    public class D1 : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }

                    public class D2 : D1
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void
            When_property_in_mode_AtHighestSourceInTypeHierarchy_with_external_base_at_top_it_must_report_at_highest_in_source()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public abstract class B
                    {
                        public abstract string P { get; }
                    }")
                .InGlobalScope(@"
                    public class D1 : B
                    {
                        <annotate/> public override string [|P|]
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }

                    public class D2 : D1
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_in_mode_AtTopInTypeHierarchy_with_interface_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .InGlobalScope(@"
                    public interface I
                    {
                        <annotate/> string [|P|] { get; }
                    }

                    public abstract class B : I
                    {
                        public abstract string P { get; }
                    }

                    public class D : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_in_mode_AtTopInTypeHierarchy_with_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        string P { get; }
                    }")
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract string P { get; }
                    }

                    public class D : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_mode_AtTopInTypeHierarchy_with_annotated_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        string P { get; }
                    }")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:I.P")
                        .NotNull()))
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract string P { get; }
                    }

                    public class D : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_property_in_mode_AtTopInTypeHierarchy_with_base_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .InGlobalScope(@"
                    public abstract class B
                    {
                        <annotate/> public abstract string [|P|] { get; }
                    }

                    public class D1 : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }

                    public class D2 : D1
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_in_mode_AtTopInTypeHierarchy_with_external_base_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public abstract class B
                    {
                        public abstract string P { get; }
                    }")
                .InGlobalScope(@"
                    public class D1 : B
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }

                    public class D2 : D1
                    {
                        public override string P
                        {
                            get { throw new NotImplementedException(); }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }
    }
}
