using VesselStowage.Domain;

namespace VesselStowage.Domain.Tests.Generators;

// Written by hand, beside the scaffolded file: `any generate VesselProfile --force` leaves it alone.
public sealed partial class AnyVesselProfile {

    /// <summary>
    ///     A vessel no limit of which a handful of boxes can reach: at least 6 bays, 7 rows and 4 tiers in the hold
    ///     and on deck, stack, displacement, vertical and transverse limits far above a few dozen boxes.
    ///     A test that is about one of those limits pins it afterwards.
    /// </summary>
    public AnyVesselProfile Roomy() {
        return WithBayCount(bays => bays.GreaterThanOrEqualTo(6))
              .WithRowCount(rows => rows.GreaterThanOrEqualTo(7))
              .WithHoldTiers(tiers => tiers.GreaterThanOrEqualTo(4))
              .WithDeckTiers(tiers => tiers.GreaterThanOrEqualTo(4))
              .WithMaxStackWeightTonnes(limit => limit.GreaterThanOrEqualTo(5_000m))
              .WithLightshipTonnes(lightship => lightship.Between(1_000m, 100_000m))
              .WithMaxDisplacementTonnes(limit => limit.GreaterThanOrEqualTo(10_000_000m))
              .WithLightshipVcgMetres(vcg => vcg.Between(1.0, 30.0))
              .WithMaxVcgMetres(limit => limit.GreaterThanOrEqualTo(1_000.0))
              .WithMaxTcgMetres(limit => limit.GreaterThanOrEqualTo(100.0));
    }

}
