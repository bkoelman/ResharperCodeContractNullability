﻿using System.Collections.Generic;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    /// <summary />
    internal sealed class MemberSourceCodeBuilder : SourceCodeBuilder
    {
        [NotNull]
        [ItemNotNull]
        private readonly List<string> members = new List<string>();

        protected override string GetSourceCode()
        {
            var builder = new StringBuilder();

            AppendClassStart(builder);
            AppendClassMembers(builder);
            AppendClassEnd(builder);

            return builder.ToString();
        }

        private static void AppendClassStart([NotNull] StringBuilder builder)
        {
            builder.AppendLine("public class Test");
            builder.AppendLine("{");
        }

        private void AppendClassMembers([NotNull] StringBuilder builder)
        {
            string code = GetLinesOfCode(members);
            builder.AppendLine(code);
        }

        private static void AppendClassEnd([NotNull] StringBuilder builder)
        {
            builder.AppendLine("}");
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
