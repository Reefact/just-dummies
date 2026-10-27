namespace JustDummies.Documentation.UnitTests.Fixtures;

/// <summary>
///     The part of <see cref="AnyShipment" /> written by hand beside the scaffolded file, exactly as the
///     <c>justdummies-cli</c> page teaches: the one overload the tool does not write, for a test that wants the
///     declared value <see langword="null" /> some of the time (ADR-0109).
/// </summary>
public sealed partial class AnyShipment {

    /// <summary>Draws <c>declaredValue</c> from <paramref name="generator" />, in place of its valid base.</summary>
    public AnyShipment WithDeclaredValue(IAny<decimal?> generator) {
        return new AnyShipment(_route, _parcels, _labels, generator);
    }

}
