namespace OrbitalMicrobiology.Tests.Expert.Arbitrary;

// Written by hand, beside the scaffolded file: `any generate --force` never touches it.
public sealed partial class AnySpecies {

    /// <summary>
    ///     Pins the three cardinal temperatures together: they are one rule (min &lt; opt &lt; max),
    ///     so a test that cares about one usually states all three.
    /// </summary>
    public AnySpecies WithCardinalTemperatures(double minimum, double optimum, double maximum) {
        return WithMinTemperatureCelsius(minimum).WithOptimalTemperatureCelsius(optimum).WithMaxTemperatureCelsius(maximum);
    }

    /// <summary>A species that starts growing as soon as it is inoculated.</summary>
    public AnySpecies WithoutLag() {
        return WithLagHours(0);
    }

}
