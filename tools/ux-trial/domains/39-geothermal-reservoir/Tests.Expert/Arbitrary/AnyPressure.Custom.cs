using GeothermalReservoir.Domain;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

public sealed partial class AnyPressure {

    /// <summary>Reservoir engineers think in bars; the domain stores pascals.</summary>
    public AnyPressure BetweenBars(double lowest, double highest) {
        return WithPascals(pascals => pascals.Between(lowest * 1e5, highest * 1e5));
    }

}
