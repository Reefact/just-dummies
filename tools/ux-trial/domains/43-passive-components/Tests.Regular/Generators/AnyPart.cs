// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Part --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Part" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPart : IAny<Part> {

    private IAny<string>                 _partNumber;
    private IAny<ComponentKind>          _kind;
    private IAny<double>                 _nominalValue;
    private IAny<Tolerance>              _tolerance;
    private IAny<double>                 _rating;
    private IAny<TemperatureCoefficient> _temperatureCoefficient;
    private IAny<Package>                _package;
    private IAny<decimal>                _unitCost;
    private bool                         _nominalValuePinned;
    private bool                         _ratingPinned;
    private bool                         _packagePinned;

    private AnyPattern                 _partNumberRecipe;
    private AnyEnum<ComponentKind>    _kindRecipe;
    private AnyDouble                 _nominalValueRecipe;
    private AnyTolerance              _toleranceRecipe;
    private AnyDouble                 _ratingRecipe;
    private AnyTemperatureCoefficient _temperatureCoefficientRecipe;
    private AnyEnum<Package>          _packageRecipe;
    private AnyDecimal                _unitCostRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPart()
        : this(partNumber:             AnyValidPartNumber(),
               kind:                   AnyValidKind(),
               nominalValue:           AnyValidNominalValue(),
               tolerance:              AnyValidTolerance(),
               rating:                 AnyValidRating(),
               temperatureCoefficient: AnyValidTemperatureCoefficient(),
               package:                AnyValidPackage(),
               unitCost:               AnyValidUnitCost()) { }

    private static AnyPattern AnyValidPartNumber() {
        return Any.StringMatching("[A-Z0-9][A-Z0-9.\\-/+#]{2,39}");
    }

    private static AnyEnum<ComponentKind> AnyValidKind() {
        return Any.Enum<ComponentKind>();
    }

    private static AnyDouble AnyValidNominalValue() {
        return Any.Double();
    }

    private static AnyTolerance AnyValidTolerance() {
        return new AnyTolerance();
    }

    private static AnyDouble AnyValidRating() {
        return Any.Double();
    }

    private static AnyTemperatureCoefficient AnyValidTemperatureCoefficient() {
        return new AnyTemperatureCoefficient();
    }

    private static AnyEnum<Package> AnyValidPackage() {
        return Any.Enum<Package>();
    }

    private static AnyDecimal AnyValidUnitCost() {
        return Any.Decimal().Between(0m, Part.MaxUnitCost).WithScale(4);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPart(AnyPattern                 partNumber,
                    AnyEnum<ComponentKind>    kind,
                    AnyDouble                 nominalValue,
                    AnyTolerance              tolerance,
                    AnyDouble                 rating,
                    AnyTemperatureCoefficient temperatureCoefficient,
                    AnyEnum<Package>          package,
                    AnyDecimal                unitCost) {
        _partNumber                   = partNumber;
        _partNumberRecipe             = partNumber;
        _kind                         = kind;
        _kindRecipe                   = kind;
        _nominalValue                 = nominalValue;
        _nominalValueRecipe           = nominalValue;
        _tolerance                    = tolerance;
        _toleranceRecipe              = tolerance;
        _rating                       = rating;
        _ratingRecipe                 = rating;
        _temperatureCoefficient       = temperatureCoefficient;
        _temperatureCoefficientRecipe = temperatureCoefficient;
        _package                      = package;
        _packageRecipe                = package;
        _unitCost                     = unitCost;
        _unitCostRecipe               = unitCost;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPart(AnyPart from) {
        _partNumber                   = from._partNumber;
        _partNumberRecipe             = from._partNumberRecipe;
        _kind                         = from._kind;
        _kindRecipe                   = from._kindRecipe;
        _nominalValue                 = from._nominalValue;
        _nominalValueRecipe           = from._nominalValueRecipe;
        _tolerance                    = from._tolerance;
        _toleranceRecipe              = from._toleranceRecipe;
        _rating                       = from._rating;
        _ratingRecipe                 = from._ratingRecipe;
        _temperatureCoefficient       = from._temperatureCoefficient;
        _temperatureCoefficientRecipe = from._temperatureCoefficientRecipe;
        _package                      = from._package;
        _packageRecipe                = from._packageRecipe;
        _unitCost                     = from._unitCost;
        _unitCostRecipe               = from._unitCostRecipe;
        _nominalValuePinned           = from._nominalValuePinned;
        _ratingPinned                 = from._ratingPinned;
        _packagePinned                = from._packagePinned;
    }

    /// <summary>Pins <c>partNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPart WithPartNumber(string value) {
        AnyPart copy = new AnyPart(this);

        copy._partNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>partNumber</c> is drawn from — <c>AnyValidPartNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPart WithPartNumber(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPart copy = new AnyPart(this);

        copy._partNumberRecipe = configure(_partNumberRecipe);
        copy._partNumber       = copy._partNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPart WithKind(ComponentKind value) {
        AnyPart copy = new AnyPart(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ComponentKind&gt;, AnyEnum&lt;ComponentKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPart WithKind(Func<AnyEnum<ComponentKind>, AnyEnum<ComponentKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPart copy = new AnyPart(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>nominalValue</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPart WithNominalValue(double value) {
        AnyPart copy = new AnyPart(this);

        copy._nominalValue       = Any.Fixed(value);
        copy._nominalValuePinned = true;

        return copy;
    }

    /// <summary>Pins <c>tolerance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPart WithTolerance(Tolerance value) {
        AnyPart copy = new AnyPart(this);

        copy._tolerance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tolerance</c> is drawn from — <c>AnyValidTolerance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTolerance, AnyTolerance&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPart WithTolerance(Func<AnyTolerance, AnyTolerance> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPart copy = new AnyPart(this);

        copy._toleranceRecipe = configure(_toleranceRecipe);
        copy._tolerance       = copy._toleranceRecipe;

        return copy;
    }

    /// <summary>Pins <c>rating</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPart WithRating(double value) {
        AnyPart copy = new AnyPart(this);

        copy._rating       = Any.Fixed(value);
        copy._ratingPinned = true;

        return copy;
    }

    /// <summary>Pins <c>temperatureCoefficient</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPart WithTemperatureCoefficient(TemperatureCoefficient value) {
        AnyPart copy = new AnyPart(this);

        copy._temperatureCoefficient = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>temperatureCoefficient</c> is drawn from — <c>AnyValidTemperatureCoefficient()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperatureCoefficient, AnyTemperatureCoefficient&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPart WithTemperatureCoefficient(Func<AnyTemperatureCoefficient, AnyTemperatureCoefficient> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPart copy = new AnyPart(this);

        copy._temperatureCoefficientRecipe = configure(_temperatureCoefficientRecipe);
        copy._temperatureCoefficient       = copy._temperatureCoefficientRecipe;

        return copy;
    }

    /// <summary>Pins <c>package</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPart WithPackage(Package value) {
        AnyPart copy = new AnyPart(this);

        copy._package       = Any.Fixed(value);
        copy._packagePinned = true;

        return copy;
    }

    /// <summary>Pins <c>unitCost</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPart WithUnitCost(decimal value) {
        AnyPart copy = new AnyPart(this);

        copy._unitCost = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unitCost</c> is drawn from — <c>AnyValidUnitCost()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPart WithUnitCost(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPart copy = new AnyPart(this);

        copy._unitCostRecipe = configure(_unitCostRecipe);
        copy._unitCost       = copy._unitCostRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Part" />.</summary>
    public Part Generate() {
        // The kind comes first: the package, the nominal value and the rating are all valid for a kind (and a package).
        ComponentKind kind         = _kind.Generate();
        Package       package      = _packagePinned ? _package.Generate() : AnyValidPackageFor(kind).Generate();
        double        nominalValue = _nominalValuePinned ? _nominalValue.Generate() : AnyValidNominalValueFor(kind).Generate();
        double        rating       = _ratingPinned ? _rating.Generate() : AnyValidRatingFor(kind, package).Generate();

        return new Part(_partNumber.Generate(),
                        kind,
                        nominalValue,
                        _tolerance.Generate(),
                        rating,
                        _temperatureCoefficient.Generate(),
                        package,
                        _unitCost.Generate());
    }

    private static IAny<Package> AnyValidPackageFor(ComponentKind kind) {
        return kind == ComponentKind.Resistor
            ? Any.Enum<Package>().Except(Package.Radial)
            : Any.Enum<Package>().Except(Package.Chip2512);
    }

    private static IAny<double> AnyValidNominalValueFor(ComponentKind kind) {
        return kind switch {
            ComponentKind.Capacitor => Magnitudes.AcrossDecades(-13, -2),
            ComponentKind.Inductor  => Magnitudes.AcrossDecades(-9, 0),
            _                       => Magnitudes.AcrossDecades(-3, 12)
        };
    }

    private static IAny<double> AnyValidRatingFor(ComponentKind kind, Package package) {
        return kind switch {
            ComponentKind.Capacitor => Any.Double().Between(Part.MinRatedVolts, Part.MaxRatedVolts),
            ComponentKind.Inductor  => Any.Double().Between(Part.MinRatedAmperes, Part.MaxRatedAmperes),
            // a resistor has no radial package: leave the refusal to the constructor, not to this draw
            _                       => Any.Double().Between(Part.MinRatedWatts, package == Package.Radial ? Part.MinRatedWatts * 2 : Part.MaxRatedWatts(package))
        };
    }

}
