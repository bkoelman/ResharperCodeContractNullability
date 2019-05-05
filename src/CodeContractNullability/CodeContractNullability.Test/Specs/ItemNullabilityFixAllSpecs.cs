using System.Collections.Generic;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    public sealed class ItemNullabilityFixAllSpecs : ItemNullabilityTest
    {
        [Fact]
        public void When_parameter_item_types_are_reference_it_must_all_be_reported_and_fixed()
        {
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        void M([+NullabilityAttributePlaceholder+] IEnumerable<string> [|p1|], [+NullabilityAttributePlaceholder+] IEnumerable<string> [|p2|])
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
