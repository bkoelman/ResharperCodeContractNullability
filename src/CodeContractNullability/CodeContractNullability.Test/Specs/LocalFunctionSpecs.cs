using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on local functions.
    /// </summary>
    public sealed class LocalFunctionSpecs : NullabilityTest
    {
#if !NET45
        [Fact]
        public void When_local_function_parameter_is_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    public void M()
                    {
                        void L(string s)
                        {
                            throw new NotImplementedException();
                        }

                        L(null);
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Fact]
        public void When_local_function_return_value_is_nullable_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    public void M()
                    {
                        string L()
                        {
                            throw new NotImplementedException();
                        }

                        string s = L();
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }
#endif
    }
}
