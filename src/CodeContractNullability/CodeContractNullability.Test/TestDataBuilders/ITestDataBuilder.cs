using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public interface ITestDataBuilder<out T>
    {
        [NotNull]
        // ReSharper disable once UnusedMemberInSuper.Global
        T Build();
    }
}
