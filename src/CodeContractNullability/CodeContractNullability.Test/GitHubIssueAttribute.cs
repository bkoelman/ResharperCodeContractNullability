using System;

namespace CodeContractNullability.Test
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class GitHubIssueAttribute : Attribute
    {
        public int Id { get; private set; }

        public GitHubIssueAttribute(int id)
        {
            Id = id;
        }
    }
}