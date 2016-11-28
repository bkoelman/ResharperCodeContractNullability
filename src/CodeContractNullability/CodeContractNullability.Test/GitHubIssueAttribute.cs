using System;

namespace CodeContractNullability.Test
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    internal sealed class GitHubIssueAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Id { get; private set; }

        internal GitHubIssueAttribute(int id)
        {
            Id = id;
        }
    }
}
