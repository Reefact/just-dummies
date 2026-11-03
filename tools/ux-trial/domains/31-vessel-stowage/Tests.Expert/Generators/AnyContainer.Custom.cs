using VesselStowage.Domain;

namespace VesselStowage.Domain.Tests.Generators;

// Written by hand, beside the scaffolded file: `any generate Container --force` leaves it alone.
public sealed partial class AnyContainer {

    /// <summary>A box with no dangerous goods, for the tests where segregation is not what is being verified.</summary>
    public AnyContainer GeneralCargo() {
        return WithHazard(HazardClass.None);
    }

    public AnyContainer OfTwentyFeet() {
        return WithSize(ContainerSize.TwentyFoot);
    }

    public AnyContainer OfFortyFeet() {
        return WithSize(ContainerSize.FortyFoot);
    }

    public AnyContainer Sailing(PortCode loadedAt, PortCode dischargedAt) {
        return WithPortOfLoading(loadedAt).WithPortOfDischarge(dischargedAt);
    }

    public AnyContainer Weighing(decimal grossMassTonnes) {
        return WithGrossMassTonnes(grossMassTonnes);
    }

    public AnyContainer Carrying(HazardClass hazard) {
        return WithHazard(hazard);
    }

}
