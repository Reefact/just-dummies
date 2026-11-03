using JustDummies;

namespace Insurance.Domain;

public sealed partial class AnyClaim {

    /// <summary>A claim occurring on any day of <paramref name="period" />.</summary>
    public AnyClaim During(CoveragePeriod period) {
        return WithOccurredOn(day => day.Between(period.Start, period.End));
    }

}
