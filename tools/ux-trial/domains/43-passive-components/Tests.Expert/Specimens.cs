using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

namespace PassiveComponents.Domain.Tests.Expert;

/// <summary>
///     Named starting points for tests about substitution. Each pins what the substitution rule reads, so that a test
///     varies exactly the one attribute it is about; the part number and the cost stay arbitrary.
/// </summary>
internal static class Specimens {

    /// <summary>A 100 Ω ±5 % thick-film resistor, 0.1 W in 1206, 100 ppm/K.</summary>
    public static AnyPart OriginalResistor() {
        return Any.Part()
                  .WithKind(ComponentKind.Resistor)
                  .WithPackage(Package.Chip1206)
                  .WithNominalValue(100)
                  .WithTolerance(new Tolerance(5))
                  .WithRating(0.1)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(100));
    }

    /// <summary>A 4.7 pF ±5 % C0G capacitor, 50 V in 0402, 30 ppm/K.</summary>
    public static AnyPart OriginalCapacitor() {
        return Any.Part()
                  .WithKind(ComponentKind.Capacitor)
                  .WithPackage(Package.Chip0402)
                  .WithNominalValue(4.7e-12)
                  .WithTolerance(new Tolerance(5))
                  .WithRating(50)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(30));
    }

}
