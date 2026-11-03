using VesselStowage.Domain;

namespace VesselStowage.Domain.Tests.Generators;

// Written by hand, beside the scaffolded file: `any generate SlotPosition --force` leaves it alone.
public sealed partial class AnySlotPosition {

    /// <summary>A slot in a bay that takes boxes of the given size: odd bays hold 20 ft, even bays 40 ft.</summary>
    public AnySlotPosition Accepting(ContainerSize size) {
        return WithBay(bay => size == ContainerSize.TwentyFoot ? bay.Odd() : bay.Even());
    }

}
