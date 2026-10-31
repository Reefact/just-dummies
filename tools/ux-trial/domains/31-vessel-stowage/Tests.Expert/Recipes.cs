// Domain vocabulary for the scaffolded generators: each recipe names a prerequisite a scenario
// needs, so a test reads as stowage language rather than as a list of constraints.

using System;
using System.Linq;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>The least geometry and the loosest limits a "roomy" vessel guarantees.</summary>
public static class RoomyVessel {

    public const int MinBays       = 20;
    public const int MinRows       = 8;
    public const int MinHoldTiers  = 4;
    public const int MinDeckTiers  = 4;

}

public sealed partial class AnyVesselProfile {

    /// <summary>
    ///     A vessel whose geometry holds every slot <see cref="AnySlotPosition.InRoomyVessel" /> draws and
    ///     whose stack, displacement and stability limits a handful of boxes cannot reach.
    /// </summary>
    public AnyVesselProfile Roomy() {
        return WithBayCount(b => b.GreaterThanOrEqualTo(RoomyVessel.MinBays))
              .WithRowCount(r => r.GreaterThanOrEqualTo(RoomyVessel.MinRows))
              .WithHoldTiers(t => t.GreaterThanOrEqualTo(RoomyVessel.MinHoldTiers))
              .WithDeckTiers(t => t.GreaterThanOrEqualTo(RoomyVessel.MinDeckTiers))
              .WithMaxStackWeightTonnes(w => w.GreaterThanOrEqualTo(200m))
              .WithLightshipTonnes(l => l.LessThanOrEqualTo(20_000m))
              .WithMaxVcgMetres(v => v.GreaterThanOrEqualTo(20.0))
              .WithMaxTcgMetres(t => t.GreaterThanOrEqualTo(5.0));
    }

}

public sealed partial class AnySlotPosition {

    /// <summary>A slot of a bay taking <paramref name="size" /> boxes, inside any roomy vessel.</summary>
    public AnySlotPosition InRoomyVessel(ContainerSize size) {
        int   parity = size == ContainerSize.TwentyFoot ? 1 : 0;
        int[] bays   = Enumerable.Range(1, RoomyVessel.MinBays).Where(b => b % 2 == parity).ToArray();

        return WithBay(b => b.OneOf(bays))
              .WithRow(r => r.LessThan(RoomyVessel.MinRows))
              // The base already draws OneOf(every valid tier); a second OneOf is refused, so narrow it.
              .WithTier(t => t.Except(10, 12, 14, 16, 18, 20, 90, 92, 94, 96, 98));
    }

    /// <summary>The bottom tier of the hold, which needs no box below it.</summary>
    public AnySlotPosition AtFootOfHold() => WithTier(2);

    /// <summary>The bottom tier on deck, standing on the hatch cover.</summary>
    public AnySlotPosition AtFootOfDeck() => WithTier(82);

}

public sealed partial class AnyContainer {

    /// <summary>Booked from <paramref name="portOfLoading" /> to <paramref name="portOfDischarge" />.</summary>
    public AnyContainer Travelling(PortCode portOfLoading, PortCode portOfDischarge) {
        return WithPortOfLoading(portOfLoading).WithPortOfDischarge(portOfDischarge);
    }

    /// <summary>
    ///     Cargo that conflicts with no neighbour and may go under deck: general cargo or any class
    ///     but explosives, oxidizers and radioactive material.
    /// </summary>
    public AnyContainer SegregationNeutral() {
        return WithHazard(h => h.Except(HazardClass.Explosives, HazardClass.Oxidizers, HazardClass.Radioactive));
    }

    /// <summary>A box of the size the slot's bay takes.</summary>
    public AnyContainer FitFor(SlotPosition slot) => WithSize(slot.AcceptedSize);

    /// <summary>A box that may stand on <paramref name="lower" /> by weight: same size, no heavier.</summary>
    public AnyContainer StackableOn(Container lower) {
        return WithSize(lower.Size).WithGrossMassTonnes(g => g.LessThanOrEqualTo(lower.GrossMassTonnes));
    }

    /// <summary>A loaded box, heavy enough for any empty-ish box of its size to stand on it.</summary>
    public AnyContainer Laden() => WithGrossMassTonnes(g => g.GreaterThanOrEqualTo(10m));

}

public static class SlotPositionExtensions {

    public static SlotPosition Above(this SlotPosition slot) => new(slot.Bay, slot.Row, slot.Tier + 2);

}
