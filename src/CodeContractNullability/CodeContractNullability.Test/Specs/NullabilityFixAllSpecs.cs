using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    public sealed class NullabilityFixAllSpecs : NullabilityTest
    {
        [Fact]
        public void When_parameter_types_are_reference_it_must_all_be_reported_and_fixed()
        {
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        void M([+NullabilityAttributePlaceholder+] string [|p1|], [+NullabilityAttributePlaceholder+] string [|p2|])
                        {
                        }
                    }
                ")
                .Build();

            VerifyNullabilityFixes(source,
                CreateMessageForParameter("p1"),
                CreateMessageForParameter("p2"));
        }
    }
}
