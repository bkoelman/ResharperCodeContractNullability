using JetBrains.Annotations;

namespace CodeContractNullability.Utilities
{
    public interface ICacheProvider<out T>
    {
        [NotNull]
        T GetValue();
    }
}
