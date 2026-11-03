using VesselStowage.Domain;

namespace VesselStowage.Tests;

/// <summary>Shared fixtures: four ports in a fixed call order, a vessel with room for the scenario, plain boxes.</summary>
internal static class Scene {

    public static readonly PortCode Rotterdam = new PortCode("NLRTM");
    public static readonly PortCode Hamburg   = new PortCode("DEHAM");
    public static readonly PortCode Felixstowe = new PortCode("GBFXT");
    public static readonly PortCode LeHavre   = new PortCode("FRLEH");

    /// <summary>Rotterdam, then Hamburg, then Felixstowe, then Le Havre.</summary>
    public static Rotation FourPortRotation() {
        return new Rotation(new List<PortCode> { Rotterdam, Hamburg, Felixstowe, LeHavre });
    }

    /// <summary>
    ///     A vessel big enough for the scenarios below: a few bays, rows and tiers, and generous limits.
    ///     Name and exact geometry beyond that minimum stay arbitrary.
    /// </summary>
    public static AnyVesselProfile RoomyVessel() {
        return Any.VesselProfile()
                  .WithBayCount(b => b.GreaterThanOrEqualTo(4))
                  .WithRowCount(r => r.GreaterThanOrEqualTo(5))
                  .WithHoldTiers(t => t.GreaterThanOrEqualTo(3))
                  .WithDeckTiers(t => t.GreaterThanOrEqualTo(2))
                  .WithMaxStackWeightTonnes(500m)
                  .WithLightshipTonnes(10_000m)
                  .WithMaxDisplacementTonnes(100_000m)
                  .WithLightshipVcgMetres(10.0)
                  .WithMaxVcgMetres(30.0)
                  .WithMaxTcgMetres(5.0);
    }

    public static StowagePlan PlanOnRoomyVessel() {
        return new StowagePlan(RoomyVessel().Generate(), FourPortRotation());
    }

    /// <summary>A 20-foot general-cargo box travelling between two ports.</summary>
    public static AnyContainer Box(PortCode from, PortCode to) {
        return Any.Container()
                  .WithSize(ContainerSize.TwentyFoot)
                  .WithHazard(HazardClass.None)
                  .WithPortOfLoading(from)
                  .WithPortOfDischarge(to);
    }

    /// <summary>A 20-foot general-cargo box loaded at Rotterdam, discharged at Hamburg, of the given gross mass.</summary>
    public static Container BoxOfMass(decimal grossMassTonnes) {
        return Box(Rotterdam, Hamburg).WithGrossMassTonnes(grossMassTonnes).Generate();
    }

    public static Container Dangerous(HazardClass hazard) {
        return Box(Rotterdam, Hamburg).WithHazard(hazard).WithIsReefer(false).Generate();
    }

}
