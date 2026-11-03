using JustDummies;

namespace Serialisation.Domain;

// Written by hand, beside the generated file: `any generate Batch --force` leaves it alone.
public sealed partial class AnyBatch {

    /// <summary>A batch planned for at least <paramref name="packs" /> packs, so a test can commission that many without filling it.</summary>
    public AnyBatch WithRoomFor(int packs) {
        return WithPlannedQuantity(quantity => quantity.GreaterThanOrEqualTo(packs));
    }

}
