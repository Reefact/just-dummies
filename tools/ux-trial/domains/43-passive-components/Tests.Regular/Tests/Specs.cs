namespace PassiveComponents.Tests.Regular;

/// <summary>
///     Parts whose specification a substitution is judged on. Everything a rule does not look at (the
///     part number, the cost) stays drawn; the rest is stated at the call site.
/// </summary>
internal static class Specs {

    /// <summary>A 0603 resistor: value, tolerance in percent, rating in watts, coefficient in ppm/K.</summary>
    public static AnyPart Resistor(double ohms, double tolerancePercent, double watts, double ppm) {
        return Any.Part().Resistor()
                  .WithPackage(Package.Chip0603)
                  .WithNominalValue(ohms)
                  .WithTolerance(new Tolerance(tolerancePercent))
                  .WithRating(watts)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(ppm));
    }

    /// <summary>A 0603 capacitor: value in farads, tolerance in percent, rating in volts; 100 ppm/K.</summary>
    public static AnyPart Capacitor(double farads, double tolerancePercent, double volts) {
        return Any.Part().Capacitor()
                  .WithPackage(Package.Chip0603)
                  .WithNominalValue(farads)
                  .WithTolerance(new Tolerance(tolerancePercent))
                  .WithRating(volts)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(100));
    }

    /// <summary>A radial inductor: value in henries, rating in amperes; +-10 %, 100 ppm/K.</summary>
    public static AnyPart Inductor(double henries, double amperes) {
        return Any.Part().Inductor()
                  .WithPackage(Package.Radial)
                  .WithNominalValue(henries)
                  .WithTolerance(new Tolerance(10))
                  .WithRating(amperes)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(100));
    }

    public static ReferenceDesignator Designator(string value) => new ReferenceDesignator(value);

}
