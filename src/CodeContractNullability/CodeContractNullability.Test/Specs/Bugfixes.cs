using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    [TestFixture]
    internal class Bugfixes : NullabilityNUnitRoslynTest
    {
        [Test]
        [GitHubIssue(4)]
        public void
            When_deriving_constructed_arrays_from_externally_annotated_interface_with_open_array_types_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    public interface I<T>
                    {
                        T[] P { get; }
                        T[] M(T[] p, int i);
                    }
                    public class C : I<string>
                    {
                        public string[] P { get { throw new NotImplementedException(); } }
                        public string[] M(string[] p, int i) { throw new NotImplementedException(); }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:I`1.P")
                        .NotNull())
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:I`1.M(`0[],System.Int32)")
                        .NotNull()
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .NotNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        [GitHubIssue(4)]
        public void
            When_deriving_constructed_arrays_from_externally_annotated_class_with_open_array_types_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    public abstract class B<T>
                    {
                        public abstract T[] P { get; }
                        public abstract T[] M(T[] p, int i);
                    }
                    public class D : B<string>
                    {
                        public override string[] P { get { throw new NotImplementedException(); } }
                        public override string[] M(string[] p, int i) { throw new NotImplementedException(); }
                    }
                ")
                .ExternallyAnnotated(new ExternalAnnotationsBuilder()
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("P:B`1.P")
                        .NotNull())
                    .IncludingMember(new ExternalAnnotationFragmentBuilder()
                        .Named("M:B`1.M(`0[],System.Int32)")
                        .NotNull()
                        .WithParameter(new ExternalAnnotationParameterBuilder()
                            .Named("p")
                            .NotNull())))
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        [GitHubIssue(13)]
        public void
            When_parameter_in_nongeneric_class_that_derives_from_generic_base_class_that_implements_annotated_generic_interface_it_must_be_skipped
            ()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    public interface IErrorDemoInterface<T>
                    {
                        void DoSomething([NotNull] T text);
                    }
                    public class ErrorDemoBase<T> : IErrorDemoInterface<T>
                    {
                        public virtual void DoSomething(T text)
                        {
                        }
                    }
                    public class ErrorDemo : ErrorDemoBase<string>
                    {
                        public override void DoSomething(string text)
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }
    }
}