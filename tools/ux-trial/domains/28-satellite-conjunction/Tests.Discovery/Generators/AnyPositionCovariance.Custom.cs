namespace SatelliteConjunction.Domain;

// Written by hand, beside the scaffolded file: `any generate PositionCovariance --force` leaves it alone.
public sealed partial class AnyPositionCovariance {

    /// <summary>
    ///     Variances small enough that the sum of any two such covariances is still a valid one. The domain accepts
    ///     variances up to 1000 km squared each yet refuses the combination of two covariances whose sum exceeds that,
    ///     so a conjunction drawn from the full domain could not even be assessed.
    /// </summary>
    public AnyPositionCovariance Combinable() {
        double half = PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm / 2;

        return WithXx(variance => variance.LessThanOrEqualTo(half))
              .WithYy(variance => variance.LessThanOrEqualTo(half))
              .WithZz(variance => variance.LessThanOrEqualTo(half));
    }

}
