// Scaffolded by `any` (JustDummies), then rewritten by hand. The scaffold marked five of the eight parameters
// "to verify": kind, package, nominal value and rating are tied together by guards (package fits the kind, the value
// range follows the kind, the rating range follows kind and package), and the part number is a Regex. The tie is
// written once, in GenerateArguments(); the independent parameters keep the scaffold's pin and refine pairs.
//
// The part number is drawn at least ten characters long, from 36^10 possibilities: two dummy parts then never share
// a part number by accident. A test about the 3-character floor writes its part numbers as literals.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

public sealed partial class AnyPart : IAny<Part> {

    private static readonly IReadOnlyList<(ComponentKind Kind, Package Package)> ValidPairs = BuildValidPairs();

    private IAny<string>                 _partNumber;
    private IAny<Tolerance>              _tolerance;
    private IAny<TemperatureCoefficient> _temperatureCoefficient;
    private IAny<decimal>                _unitCost;

    private AnyPattern                    _partNumberRecipe;
    private AnyTolerance                  _toleranceRecipe;
    private AnyTemperatureCoefficient     _temperatureCoefficientRecipe;
    private AnyDecimal                    _unitCostRecipe;

    private ComponentKind? _kindPin;
    private Package?       _packagePin;
    private double?        _nominalValuePin;
    private double?        _ratingPin;

    public AnyPart() {
        _partNumberRecipe             = Any.StringMatching("[A-Z0-9][A-Z0-9.\\-/+#]{9,39}");
        _toleranceRecipe              = new AnyTolerance();
        _temperatureCoefficientRecipe = new AnyTemperatureCoefficient();
        _unitCostRecipe               = Any.Decimal().Between(0m, Part.MaxUnitCost).WithScale(4);
        _partNumber                   = _partNumberRecipe;
        _tolerance                    = _toleranceRecipe;
        _temperatureCoefficient       = _temperatureCoefficientRecipe;
        _unitCost                     = _unitCostRecipe;
    }

    private AnyPart(AnyPart from) {
        _partNumber                   = from._partNumber;
        _partNumberRecipe             = from._partNumberRecipe;
        _tolerance                    = from._tolerance;
        _toleranceRecipe              = from._toleranceRecipe;
        _temperatureCoefficient       = from._temperatureCoefficient;
        _temperatureCoefficientRecipe = from._temperatureCoefficientRecipe;
        _unitCost                     = from._unitCost;
        _unitCostRecipe               = from._unitCostRecipe;
        _kindPin                      = from._kindPin;
        _packagePin                   = from._packagePin;
        _nominalValuePin              = from._nominalValuePin;
        _ratingPin                    = from._ratingPin;
    }

    public AnyPart WithPartNumber(string value) {
        AnyPart copy = new AnyPart(this);

        copy._partNumber = Any.Fixed(value);

        return copy;
    }

    public AnyPart WithPartNumber(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPart copy = new AnyPart(this);

        copy._partNumberRecipe = configure(_partNumberRecipe);
        copy._partNumber       = copy._partNumberRecipe;

        return copy;
    }

    /// <summary>Restricts the draw to one kind; the package, nominal value and rating follow from it.</summary>
    public AnyPart WithKind(ComponentKind value) {
        AnyPart copy = new AnyPart(this);

        copy._kindPin = value;
        copy.RequireAValidPair();

        return copy;
    }

    /// <summary>Restricts the draw to one package; the kind must be one that comes in it.</summary>
    public AnyPart WithPackage(Package value) {
        AnyPart copy = new AnyPart(this);

        copy._packagePin = value;
        copy.RequireAValidPair();

        return copy;
    }

    public AnyPart WithNominalValue(double value) {
        AnyPart copy = new AnyPart(this);

        copy._nominalValuePin = value;

        return copy;
    }

    public AnyPart WithRating(double value) {
        AnyPart copy = new AnyPart(this);

        copy._ratingPin = value;

        return copy;
    }

    public AnyPart WithTolerance(Tolerance value) {
        AnyPart copy = new AnyPart(this);

        copy._tolerance = Any.Fixed(value);

        return copy;
    }

    public AnyPart WithTolerance(Func<AnyTolerance, AnyTolerance> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPart copy = new AnyPart(this);

        copy._toleranceRecipe = configure(_toleranceRecipe);
        copy._tolerance       = copy._toleranceRecipe;

        return copy;
    }

    public AnyPart WithTemperatureCoefficient(TemperatureCoefficient value) {
        AnyPart copy = new AnyPart(this);

        copy._temperatureCoefficient = Any.Fixed(value);

        return copy;
    }

    public AnyPart WithUnitCost(decimal value) {
        AnyPart copy = new AnyPart(this);

        copy._unitCost = Any.Fixed(value);

        return copy;
    }

    /// <summary>The eight arguments of one valid part, for a test that changes one of them.</summary>
    public PartArguments GenerateArguments() {
        (ComponentKind kind, Package package) = Any.ElementOf(MatchingPairs()).Generate();

        double nominalValue = _nominalValuePin ?? NominalValueRecipeOf(kind).Generate();
        double rating       = _ratingPin ?? RatingRecipeOf(kind, package).Generate();

        return new PartArguments(_partNumber.Generate(),
                                 kind,
                                 nominalValue,
                                 _tolerance.Generate(),
                                 rating,
                                 _temperatureCoefficient.Generate(),
                                 package,
                                 _unitCost.Generate());
    }

    public Part Generate() {
        return GenerateArguments().Build();
    }

    private List<(ComponentKind Kind, Package Package)> MatchingPairs() {
        return ValidPairs.Where(pair => (_kindPin is null || pair.Kind == _kindPin) && (_packagePin is null || pair.Package == _packagePin)).ToList();
    }

    private void RequireAValidPair() {
        if (MatchingPairs().Count == 0) {
            throw new ArgumentException($"No part is a {_kindPin} in a {_packagePin} package.");
        }
    }

    private static IAny<double> NominalValueRecipeOf(ComponentKind kind) {
        return kind switch {
            ComponentKind.Capacitor => LogScale.Between(Capacitance.MinFarads, Capacitance.MaxFarads),
            ComponentKind.Inductor  => LogScale.Between(Inductance.MinHenries, Inductance.MaxHenries),
            _                       => LogScale.Between(Resistance.MinOhms, Resistance.MaxOhms),
        };
    }

    private static IAny<double> RatingRecipeOf(ComponentKind kind, Package package) {
        return kind switch {
            ComponentKind.Capacitor => LogScale.Between(Part.MinRatedVolts, Part.MaxRatedVolts),
            ComponentKind.Inductor  => LogScale.Between(Part.MinRatedAmperes, Part.MaxRatedAmperes),
            _                       => Any.Double().Between(Part.MinRatedWatts, Part.MaxRatedWatts(package)),
        };
    }

    private static List<(ComponentKind Kind, Package Package)> BuildValidPairs() {
        List<(ComponentKind Kind, Package Package)> pairs = new();

        foreach (ComponentKind kind in Enum.GetValues<ComponentKind>()) {
            foreach (Package package in Enum.GetValues<Package>()) {
                bool resistorOnly   = package == Package.Chip2512;
                bool nonResistorOnly = package == Package.Radial;

                if (resistorOnly && kind != ComponentKind.Resistor) { continue; }
                if (nonResistorOnly && kind == ComponentKind.Resistor) { continue; }

                pairs.Add((kind, package));
            }
        }

        return pairs;
    }

}
