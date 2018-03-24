using System.Collections.Generic;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public sealed class MemberSourceCodeBuilder : SourceCodeBuilder
    {
        [NotNull]
        [ItemNotNull]
        private readonly List<string> members = new List<string>();

        protected override string GetSourceCode()
        {
            var builder = new StringBuilder();
            builder.AppendLine("public class Test");
            builder.AppendLine("{");

            string code = GetLinesOfCode(members);
            builder.AppendLine(code);

            builder.AppendLine("}");
            return builder.ToString();
        }

        [NotNull]
        public MemberSourceCodeBuilder InDefaultClass([NotNull] string memberCode)
        {
            Guard.NotNull(memberCode, nameof(memberCode));

            members.Add(memberCode);
            return this;
        }
    }
}
