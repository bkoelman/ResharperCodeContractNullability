using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Conversion
{
    internal sealed class ResharperNullabilitySymbolState
    {
        public ResharperNullableStatus PrimaryStatus { get; }
        public ResharperNullableStatus ItemStatus { get; }

        [NotNull]
        public static readonly ResharperNullabilitySymbolState Default =
            new ResharperNullabilitySymbolState(ResharperNullableStatus.Unspecified, ResharperNullableStatus.Unspecified);

        public ResharperNullabilitySymbolState(ResharperNullableStatus primaryStatus, ResharperNullableStatus itemStatus)
        {
            PrimaryStatus = primaryStatus;
            ItemStatus = itemStatus;
        }

        [NotNull]
        public ResharperNullabilitySymbolState ApplyOverride([NotNull] ResharperNullabilitySymbolState newState)
        {
            Guard.NotNull(newState, nameof(newState));

            ResharperNullableStatus primaryStatus = ApplyOverride(PrimaryStatus, newState.PrimaryStatus);
            ResharperNullableStatus itemStatus = ApplyOverride(ItemStatus, newState.ItemStatus);

            return new ResharperNullabilitySymbolState(primaryStatus, itemStatus);
        }

        private static ResharperNullableStatus ApplyOverride(ResharperNullableStatus currentStatus,
            ResharperNullableStatus newStatus)
        {
            return newStatus == ResharperNullableStatus.Unspecified ? currentStatus : newStatus;
        }

        [NotNull]
        public ResharperNullabilitySymbolState ClearPrimaryStatus()
        {
            return new ResharperNullabilitySymbolState(ResharperNullableStatus.Unspecified, ItemStatus);
        }

        [NotNull]
        public ResharperNullabilitySymbolState ClearItemStatus()
        {
            return new ResharperNullabilitySymbolState(PrimaryStatus, ResharperNullableStatus.Unspecified);
        }
    }
}
