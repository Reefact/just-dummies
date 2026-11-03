using JustDummies;

using PassiveComponents.Domain;

namespace PassiveComponents.Tests.Discovery;

/// <summary>
///     Parts whose specification the test is about: what is pinned here is what the rule under test reads,
///     what is left alone (the unit cost, a part number nobody compares) stays drawn.
/// </summary>
internal static class TestParts {

    public static AnyPart Resistor(string partNumber, double ohms, double tolerancePercent, double watts, double ppm, Package package) {
        return Any.Part()
                  .WithKind(ComponentKind.Resistor)
                  .WithPartNumber(partNumber)
                  .WithNominalValue(ohms)
                  .WithTolerance(new Tolerance(tolerancePercent))
                  .WithRating(watts)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(ppm))
                  .WithPackage(package);
    }

    public static AnyPart Capacitor(string partNumber, double farads, double tolerancePercent, double volts, double ppm, Package package) {
        return Any.Part()
                  .WithKind(ComponentKind.Capacitor)
                  .WithPartNumber(partNumber)
                  .WithNominalValue(farads)
                  .WithTolerance(new Tolerance(tolerancePercent))
                  .WithRating(volts)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(ppm))
                  .WithPackage(package);
    }

    public static AnyPart Inductor(string partNumber, double henries, double tolerancePercent, double amperes, double ppm, Package package) {
        return Any.Part()
                  .WithKind(ComponentKind.Inductor)
                  .WithPartNumber(partNumber)
                  .WithNominalValue(henries)
                  .WithTolerance(new Tolerance(tolerancePercent))
                  .WithRating(amperes)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(ppm))
                  .WithPackage(package);
    }

    /// <summary>The 100 ohm, 5 %, 0.1 W, 100 ppm/K, 0805 part that the substitution rule is judged against.</summary>
    public static AnyPart Designed() {
        return Resistor("RES-100", 100, 5, 0.1, 100, Package.Chip0805);
    }

    public static ReferenceDesignator Designator(string value) {
        return new ReferenceDesignator(value);
    }

}
