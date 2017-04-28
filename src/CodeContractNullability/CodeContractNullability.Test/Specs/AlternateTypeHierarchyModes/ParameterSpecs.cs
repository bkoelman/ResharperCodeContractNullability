using CodeContractNullability.Settings;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs.AlternateTypeHierarchyModes
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on method parameters with non-default type hierarchy configurations.
    /// </summary>
    public sealed class ParameterSpecs : NullabilityTest
    {
        [Fact]
        public void
            When_parameter_in_mode_EverywhereInTypeHierarchy_with_interface_hierarchy_in_source_it_must_report_everywhere()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.EverywhereInTypeHierarchy))
                .InGlobalScope(@"
                    public interface I
                    {
                        void M(string [|p|]);
                    }

                    public abstract class B : I
                    {
                        public abstract void M(string [|p|]);
                    }

                    public class D : B
                    {
                        public override void M(string [|p|]) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source, CreateMessageForParameter("p"), CreateMessageForParameter("p"),
                CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_in_mode_EverywhereInTypeHierarchy_with_external_interface_at_top_it_must_report_everywhere()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.EverywhereInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        void M(string p);
                    }")
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract void M(string [|p|]);
                    }

                    public class D : B
                    {
                        public override void M(string [|p|]) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source, CreateMessageForParameter("p"), CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_in_mode_EverywhereInTypeHierarchy_with_annotated_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.EverywhereInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        void M(string p);
                    }")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:I.M(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .NotNull())))
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract void M(string p);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_mode_EverywhereInTypeHierarchy_with_base_hierarchy_in_source_it_must_report_everywhere()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.EverywhereInTypeHierarchy))
                .InGlobalScope(@"
                    public abstract class B
                    {
                        public abstract void M(string [|p|]);
                    }

                    public class D : B
                    {
                        public override void M(string [|p|]) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source, CreateMessageForParameter("p"), CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_in_mode_EverywhereInTypeHierarchy_with_external_base_at_top_it_must_report_everywhere()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.EverywhereInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public abstract class B
                    {
                        public abstract void M(string p);
                    }")
                .InGlobalScope(@"
                    public class D : B
                    {
                        public override void M(string [|p|]) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void
            When_parameter_in_mode_AtHighestSourceInTypeHierarchy_with_interface_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .InGlobalScope(@"
                    public interface I
                    {
                        void M(<annotate/> string [|p|]);
                    }

                    public abstract class B : I
                    {
                        public abstract void M(string p);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void
            When_parameter_in_mode_AtHighestSourceInTypeHierarchy_with_external_interface_at_top_it_must_report_at_highest_in_source()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        void M(string p);
                    }")
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract void M(<annotate/> string [|p|]);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void
            When_parameter_in_mode_AtHighestSourceInTypeHierarchy_with_annotated_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        void M(string p);
                    }")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:I.M(System.String)")
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .NotNull())))
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract void M(string p);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_mode_AtHighestSourceInTypeHierarchy_with_base_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .InGlobalScope(@"
                    public abstract class B
                    {
                        public abstract void M(<annotate/> string [|p|]);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void
            When_parameter_in_mode_AtHighestSourceInTypeHierarchy_with_external_base_at_top_it_must_report_at_highest_in_source()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public abstract class B
                    {
                        public abstract void M(string p);
                    }")
                .InGlobalScope(@"
                    public class D : B
                    {
                        public override void M(<annotate/> string [|p|]) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_in_mode_AtTopInTypeHierarchy_with_interface_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .InGlobalScope(@"
                    public interface I
                    {
                        void M(<annotate/> string [|p|]);
                    }

                    public abstract class B : I
                    {
                        public abstract void M(string p);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_in_mode_AtTopInTypeHierarchy_with_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        void M(string p);
                    }")
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract void M(string p);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_mode_AtTopInTypeHierarchy_with_annotated_external_interface_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public interface I
                    {
                        void M(string p);
                    }")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:I.M")
                        .NotNull()))
                .InGlobalScope(@"
                    public abstract class B : I
                    {
                        public abstract void M(string p);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_parameter_in_mode_AtTopInTypeHierarchy_with_base_hierarchy_in_source_it_must_report_at_top()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .InGlobalScope(@"
                    public abstract class B
                    {
                        public abstract void M(<annotate/> string [|p|]);
                    }

                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_parameter_in_mode_AtTopInTypeHierarchy_with_external_base_at_top_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .WithReferenceToExternalAssemblyFor(@"
                    public abstract class B
                    {
                        public abstract void M(string p);
                    }")
                .InGlobalScope(@"
                    public class D : B
                    {
                        public override void M(string p) { throw new NotImplementedException(); }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }
    }
}
