using JustDummies;

namespace VesselStowage.Domain.Tests;

/// <summary>
///     A plan on a roomy vessel whose limits never bind, over a rotation of four ports. A test that is about a limit
///     replaces the vessel; a test about the sequence picks the ports a box loads at and discharges at.
/// </summary>
internal sealed class PlanScenario {

    public PlanScenario()
        : this(RoomyVessel()) { }

    public PlanScenario(AnyVesselProfile vessel) {
        Vessel   = vessel.Generate();
        Rotation = Any.Rotation().WithPorts(ports => ports.WithCount(4)).Generate();
        Plan     = new StowagePlan(Vessel, Rotation);
    }

    public VesselProfile Vessel { get; }

    public Rotation Rotation { get; }

    public StowagePlan Plan { get; }

    public PortCode Port(int index) {
        return Rotation.Ports[index];
    }

    /// <summary>
    ///     Enough bays, rows and tiers for every slot the tests name, and limits far from any box they stow:
    ///     the stack limit, the displacement, the VCG and the TCG are what these tests are not about.
    /// </summary>
    public static AnyVesselProfile RoomyVessel() {
        return Any.VesselProfile()
                  .WithBayCount(bays => bays.GreaterThanOrEqualTo(4))
                  .WithRowCount(rows => rows.GreaterThanOrEqualTo(5))
                  .WithHoldTiers(tiers => tiers.GreaterThanOrEqualTo(3))
                  .WithDeckTiers(tiers => tiers.GreaterThanOrEqualTo(2))
                  .WithMaxStackWeightTonnes(1_000m)
                  .WithLightshipTonnes(20_000m)
                  .WithMaxDisplacementTonnes(1_000_000m)
                  .WithMaxVcgMetres(500.0)
                  .WithMaxTcgMetres(500.0);
    }

    /// <summary>A general-cargo box (no dangerous goods) between two ports of the rotation, by their position in it.</summary>
    public Container Box(ContainerSize size, int loadsAt = 0, int dischargesAt = 3, decimal? grossTonnes = null) {
        return BoxGenerator(size, loadsAt, dischargesAt, grossTonnes).WithHazard(HazardClass.None).Generate();
    }

    public Container DangerousBox(ContainerSize size, HazardClass hazard, int loadsAt = 0, int dischargesAt = 3, decimal? grossTonnes = null) {
        return BoxGenerator(size, loadsAt, dischargesAt, grossTonnes).WithHazard(hazard).WithIsReefer(false).Generate();
    }

    /// <summary>A 20-foot general-cargo box: it goes in an odd bay.</summary>
    public Container Teu(int loadsAt = 0, int dischargesAt = 3, decimal? grossTonnes = null) {
        return Box(ContainerSize.TwentyFoot, loadsAt, dischargesAt, grossTonnes);
    }

    /// <summary>A 40-foot general-cargo box: it goes in an even bay.</summary>
    public Container Feu(int loadsAt = 0, int dischargesAt = 3, decimal? grossTonnes = null) {
        return Box(ContainerSize.FortyFoot, loadsAt, dischargesAt, grossTonnes);
    }

    public StowageAssignment Stow(Container container, int bay, int row, int tier) {
        return Plan.Assign(container, new SlotPosition(bay, row, tier));
    }

    /// <summary>A port the rotation does not call.</summary>
    public PortCode ForeignPort() {
        IAny<PortCode> notInRotation = Rotation.Ports.Aggregate((IAny<PortCode>)Any.PortCode(), (generator, port) => generator.DifferentFrom(port));

        return notInRotation.Generate();
    }

    /// <summary>Whatever box the rotation can carry: any size, any hazard, any mass, loading before it discharges.</summary>
    public Container AnyBox() {
        ContainerSize size  = Any.Enum<ContainerSize>().Generate();
        int loadsAt         = Any.Int32().Between(0, 2).Generate();
        int dischargesAt    = Any.Int32().Between(loadsAt + 1, 3).Generate();

        return BoxGenerator(size, loadsAt, dischargesAt, grossTonnes: null).Generate();
    }

    /// <summary>
    ///     A vessel of the geometry and limits the test names, the rest being dummies. Nothing here binds unless the test
    ///     lowers it.
    /// </summary>
    public static AnyVesselProfile VesselOf(int bays = 4, int rows = 5, int holdTiers = 2, int deckTiers = 2, decimal maxStackWeightTonnes = 1_000m, decimal lightshipTonnes = 20_000m, decimal maxDisplacementTonnes = 1_000_000m, double lightshipVcgMetres = 10.0, double maxVcgMetres = 500.0, double maxTcgMetres = 500.0) {
        return Any.VesselProfile()
                  .WithBayCount(bays)
                  .WithRowCount(rows)
                  .WithHoldTiers(holdTiers)
                  .WithDeckTiers(deckTiers)
                  .WithMaxStackWeightTonnes(maxStackWeightTonnes)
                  .WithLightshipTonnes(lightshipTonnes)
                  .WithMaxDisplacementTonnes(maxDisplacementTonnes)
                  .WithLightshipVcgMetres(lightshipVcgMetres)
                  .WithMaxVcgMetres(maxVcgMetres)
                  .WithMaxTcgMetres(maxTcgMetres);
    }

    private AnyContainer BoxGenerator(ContainerSize size, int loadsAt, int dischargesAt, decimal? grossTonnes) {
        AnyContainer generator = Any.Container()
                                    .WithSize(size)
                                    .WithPortOfLoading(Port(loadsAt))
                                    .WithPortOfDischarge(Port(dischargesAt));

        return grossTonnes is { } gross ? generator.WithGrossMassTonnes(gross) : generator;
    }

}
