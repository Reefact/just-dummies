using JustDummies;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

public sealed partial class AnyPositionCovariance {

    // Positive definiteness ties all six terms together, so no per-term recipe can promise it alone.
    // Keeping the matrix diagonally dominant does, by construction: every variance is at least 0.01 km²
    // and every cross term at most 0.002 km², so no correlation exceeds 0.2 and the determinant stays positive.
    private static AnyDouble AnyVariance() {
        return Any.Double().Between(0.01, 1);
    }

    private static AnyDouble AnyCrossTerm() {
        return Any.Double().Between(-0.002, 0.002);
    }

}
