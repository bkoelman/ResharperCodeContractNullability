using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal interface ITestDataBuilder<out T>
    {
        [NotNull]
        // ReSharper disable once UnusedMemberInSuper.Global
        T Build();
    }
}
