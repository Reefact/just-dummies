using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Part" /> values, scaffolded by <c>any</c> then completed by hand:
///     what the constructor ties together (kind, package, value range, rating) is drawn in order in
///     <see cref="Generate" />, kind first.
/// </summary>
public sealed partial class AnyPart : IAny<Part> {

    private IAny<string>                 _partNumber;
    private IAny<ComponentKind>          _kind;
    private IAny<Tolerance>              _tolerance;
    private IAny<TemperatureCoefficient> _temperatureCoefficient;
    private IAny<decimal>                _unitCost;

    private AnyPattern                _partNumberRecipe;
    private AnyEnum<ComponentKind>    _kindRecipe;
    private AnyTolerance              _toleranceRecipe;
    private AnyTemperatureCoefficient _temperatureCoefficientRecipe;
    private AnyDecimal                _unitCostRecipe;

    // These three depend on the kind (and the package), which is only known at Generate() time:
    // they are kept as refinements to apply to the base of the drawn kind.
    private AnyEnum<Package>                      _packageRecipe;
    private Func<AnyDouble, AnyDouble>            _nominalRefinement = r => r;
    private double?                               _nominalPin;
    private Func<AnyDouble, AnyDouble>            _ratingRefinement  = r => r;
    private double?                               _ratingPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPart() {
        _partNumberRecipe             = Any.StringMatching(@"[A-Z0-9][A-Z0-9.\-/+#]{2,39}");
        _partNumber                   = _partNumberRecipe;
        _kindRecipe                   = Any.Enum<ComponentKind>();
        _kind                         = _kindRecipe;
        _toleranceRecipe              = new AnyTolerance();
        _tolerance                    = _toleranceRecipe;
        _temperatureCoefficientRecipe = new AnyTemperatureCoefficient();
        _temperatureCoefficient       = _temperatureCoefficientRecipe;
        _unitCostRecipe               = Any.Decimal().Between(0m, Part.MaxUnitCost).WithScale(4);
        _unitCost                     = _unitCostRecipe;
        _packageRecipe                = Any.Enum<Package>();
    }

    private AnyPart(AnyPart from) {
        _partNumber                   = from._partNumber;
        _partNumberRecipe             = from._partNumberRecipe;
        _kind                         = from._kind;
        _kindRecipe                   = from._kindRecipe;
        _tolerance                    = from._tolerance;
        _toleranceRecipe              = from._toleranceRecipe;
        _temperatureCoefficient       = from._temperatureCoefficient;
        _temperatureCoefficientRecipe = from._temperatureCoefficientRecipe;
        _unitCost                     = from._unitCost;
        _unitCostRecipe               = from._unitCostRecipe;
        _packageRecipe                = from._packageRecipe;
        _nominalRefinement            = from._nominalRefinement;
        _nominalPin                   = from._nominalPin;
        _ratingRefinement             = from._ratingRefinement;
        _ratingPin                    = from._ratingPin;
    }

    public AnyPart WithPartNumber(string value) {
        AnyPart copy = new AnyPart(this);
        copy._partNumber = Any.Fixed(value);
        return copy;
    }

    public AnyPart WithKind(ComponentKind value) {
        AnyPart copy = new AnyPart(this);
        copy._kind = Any.Fixed(value);
        return copy;
    }

    public AnyPart WithKind(Func<AnyEnum<ComponentKind>, AnyEnum<ComponentKind>> configure) {
        AnyPart copy = new AnyPart(this);
        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;
        return copy;
    }

    /// <summary>Pins the nominal value, in SI base units; it must suit the kind.</summary>
    public AnyPart WithNominalValue(double value) {
        AnyPart copy = new AnyPart(this);
        copy._nominalPin = value;
        return copy;
    }

    /// <summary>Refines the nominal value's recipe, which starts as the full range of the drawn kind.</summary>
    public AnyPart WithNominalValue(Func<AnyDouble, AnyDouble> configure) {
        AnyPart copy = new AnyPart(this);
        Func<AnyDouble, AnyDouble> previous = _nominalRefinement;
        copy._nominalRefinement = r => configure(previous(r));
        copy._nominalPin        = null;
        return copy;
    }

    public AnyPart WithTolerance(Tolerance value) {
        AnyPart copy = new AnyPart(this);
        copy._tolerance = Any.Fixed(value);
        return copy;
    }

    public AnyPart WithTolerance(Func<AnyTolerance, AnyTolerance> configure) {
        AnyPart copy = new AnyPart(this);
        copy._toleranceRecipe = configure(_toleranceRecipe);
        copy._tolerance       = copy._toleranceRecipe;
        return copy;
    }

    /// <summary>Pins the rating (V, W or A by kind); it must suit the kind and package.</summary>
    public AnyPart WithRating(double value) {
        AnyPart copy = new AnyPart(this);
        copy._ratingPin = value;
        return copy;
    }

    /// <summary>Refines the rating's recipe, which starts as the full range of the drawn kind and package.</summary>
    public AnyPart WithRating(Func<AnyDouble, AnyDouble> configure) {
        AnyPart copy = new AnyPart(this);
        Func<AnyDouble, AnyDouble> previous = _ratingRefinement;
        copy._ratingRefinement = r => configure(previous(r));
        copy._ratingPin        = null;
        return copy;
    }

    public AnyPart WithTemperatureCoefficient(TemperatureCoefficient value) {
        AnyPart copy = new AnyPart(this);
        copy._temperatureCoefficient = Any.Fixed(value);
        return copy;
    }

    public AnyPart WithTemperatureCoefficient(Func<AnyTemperatureCoefficient, AnyTemperatureCoefficient> configure) {
        AnyPart copy = new AnyPart(this);
        copy._temperatureCoefficientRecipe = configure(_temperatureCoefficientRecipe);
        copy._temperatureCoefficient       = copy._temperatureCoefficientRecipe;
        return copy;
    }

    /// <summary>Pins the package: a pin that does not fit the drawn kind is refused when drawing, naming both sides.</summary>
    public AnyPart WithPackage(Package value) {
        AnyPart copy = new AnyPart(this);
        copy._packageRecipe = _packageRecipe.OneOf(value);
        return copy;
    }

    public AnyPart WithPackage(Func<AnyEnum<Package>, AnyEnum<Package>> configure) {
        AnyPart copy = new AnyPart(this);
        copy._packageRecipe = configure(_packageRecipe);
        return copy;
    }

    public AnyPart WithUnitCost(decimal value) {
        AnyPart copy = new AnyPart(this);
        copy._unitCost = Any.Fixed(value);
        return copy;
    }

    public AnyPart WithUnitCost(Func<AnyDecimal, AnyDecimal> configure) {
        AnyPart copy = new AnyPart(this);
        copy._unitCostRecipe = configure(_unitCostRecipe);
        copy._unitCost       = copy._unitCostRecipe;
        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Part" />.</summary>
    public Part Generate() {
        ComponentKind kind = _kind.Generate();

        Package package = _packageRecipe
                         .Except(kind == ComponentKind.Resistor ? Package.Radial : Package.Chip2512)
                         .Generate();

        double nominal;
        if (_nominalPin is double pinnedNominal) {
            nominal = pinnedNominal;
        } else {
            AnyDouble nominalBase = kind switch {
                ComponentKind.Capacitor => Any.Double().Between(Capacitance.MinFarads, Capacitance.MaxFarads),
                ComponentKind.Inductor  => Any.Double().Between(Inductance.MinHenries, Inductance.MaxHenries),
                _                       => Any.Double().Between(Resistance.MinOhms, Resistance.MaxOhms)
            };
            nominal = _nominalRefinement(nominalBase).Generate();
        }

        double rating;
        if (_ratingPin is double pinnedRating) {
            rating = pinnedRating;
        } else {
            AnyDouble ratingBase = kind switch {
                ComponentKind.Capacitor => Any.Double().Between(Part.MinRatedVolts, Part.MaxRatedVolts),
                ComponentKind.Inductor  => Any.Double().Between(Part.MinRatedAmperes, Part.MaxRatedAmperes),
                _                       => Any.Double().Between(Part.MinRatedWatts, Part.MaxRatedWatts(package))
            };
            rating = _ratingRefinement(ratingBase).Generate();
        }

        return new Part(_partNumber.Generate(),
                        kind,
                        nominal,
                        _tolerance.Generate(),
                        rating,
                        _temperatureCoefficient.Generate(),
                        package,
                        _unitCost.Generate());
    }

}
