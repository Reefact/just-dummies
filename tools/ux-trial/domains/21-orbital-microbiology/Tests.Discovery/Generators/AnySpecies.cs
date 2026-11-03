// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Species --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Species" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpecies : IAny<Species> {

    private IAny<string>     _binomialName;
    private IAny<GrowthRate> _maxGrowthRate;
    private IAny<double>     _minTemperatureCelsius;
    private IAny<double>     _optimalTemperatureCelsius;
    private IAny<double>     _maxTemperatureCelsius;
    private IAny<double>     _lagHours;
    private IAny<double>     _yieldCellsPerGram;

    private AnyPattern     _binomialNameRecipe;
    private AnyGrowthRate _maxGrowthRateRecipe;
    private AnyDouble     _minTemperatureCelsiusRecipe;
    private AnyDouble     _optimalTemperatureCelsiusRecipe;
    private AnyDouble     _maxTemperatureCelsiusRecipe;
    private AnyDouble     _lagHoursRecipe;
    private AnyDouble     _yieldCellsPerGramRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpecies()
        : this(binomialName:              AnyValidBinomialName(),
               maxGrowthRate:             AnyValidMaxGrowthRate(),
               minTemperatureCelsius:     AnyValidMinTemperatureCelsius(),
               optimalTemperatureCelsius: AnyValidOptimalTemperatureCelsius(),
               maxTemperatureCelsius:     AnyValidMaxTemperatureCelsius(),
               lagHours:                  AnyValidLagHours(),
               yieldCellsPerGram:         AnyValidYieldCellsPerGram()) { }

    private static AnyPattern AnyValidBinomialName() {
        return Any.StringMatching("[A-Z][a-z]{3,9} [a-z]{3,10}");
    }

    private static AnyGrowthRate AnyValidMaxGrowthRate() {
        return new AnyGrowthRate().WithPerHour(rate => rate.Positive());
    }

    private static AnyDouble AnyValidMinTemperatureCelsius() {
        return Any.Double().Between(-20d, 80d);
    }

    private static AnyDouble AnyValidOptimalTemperatureCelsius() {
        return Any.Double().Between(-20d, 80d);
    }

    private static AnyDouble AnyValidMaxTemperatureCelsius() {
        return Any.Double().Between(-20d, 80d);
    }

    private static AnyDouble AnyValidLagHours() {
        return Any.Double().Between(0d, Species.MaxLagHours);
    }

    private static AnyDouble AnyValidYieldCellsPerGram() {
        return Any.Double().Between(Species.MinYieldCellsPerGram, Species.MaxYieldCellsPerGram);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySpecies(AnyPattern     binomialName,
                       AnyGrowthRate maxGrowthRate,
                       AnyDouble     minTemperatureCelsius,
                       AnyDouble     optimalTemperatureCelsius,
                       AnyDouble     maxTemperatureCelsius,
                       AnyDouble     lagHours,
                       AnyDouble     yieldCellsPerGram) {
        _binomialName                    = binomialName;
        _binomialNameRecipe              = binomialName;
        _maxGrowthRate                   = maxGrowthRate;
        _maxGrowthRateRecipe             = maxGrowthRate;
        _minTemperatureCelsius           = minTemperatureCelsius;
        _minTemperatureCelsiusRecipe     = minTemperatureCelsius;
        _optimalTemperatureCelsius       = optimalTemperatureCelsius;
        _optimalTemperatureCelsiusRecipe = optimalTemperatureCelsius;
        _maxTemperatureCelsius           = maxTemperatureCelsius;
        _maxTemperatureCelsiusRecipe     = maxTemperatureCelsius;
        _lagHours                        = lagHours;
        _lagHoursRecipe                  = lagHours;
        _yieldCellsPerGram               = yieldCellsPerGram;
        _yieldCellsPerGramRecipe         = yieldCellsPerGram;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySpecies(AnySpecies from) {
        _binomialName                    = from._binomialName;
        _binomialNameRecipe              = from._binomialNameRecipe;
        _maxGrowthRate                   = from._maxGrowthRate;
        _maxGrowthRateRecipe             = from._maxGrowthRateRecipe;
        _minTemperatureCelsius           = from._minTemperatureCelsius;
        _minTemperatureCelsiusRecipe     = from._minTemperatureCelsiusRecipe;
        _optimalTemperatureCelsius       = from._optimalTemperatureCelsius;
        _optimalTemperatureCelsiusRecipe = from._optimalTemperatureCelsiusRecipe;
        _maxTemperatureCelsius           = from._maxTemperatureCelsius;
        _maxTemperatureCelsiusRecipe     = from._maxTemperatureCelsiusRecipe;
        _lagHours                        = from._lagHours;
        _lagHoursRecipe                  = from._lagHoursRecipe;
        _yieldCellsPerGram               = from._yieldCellsPerGram;
        _yieldCellsPerGramRecipe         = from._yieldCellsPerGramRecipe;
    }

    /// <summary>Pins <c>binomialName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpecies WithBinomialName(string value) {
        AnySpecies copy = new AnySpecies(this);

        copy._binomialName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>binomialName</c> is drawn from — <c>AnyValidBinomialName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecies WithBinomialName(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecies copy = new AnySpecies(this);

        copy._binomialNameRecipe = configure(_binomialNameRecipe);
        copy._binomialName       = copy._binomialNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>maxGrowthRate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpecies WithMaxGrowthRate(GrowthRate value) {
        AnySpecies copy = new AnySpecies(this);

        copy._maxGrowthRate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maxGrowthRate</c> is drawn from — <c>AnyValidMaxGrowthRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGrowthRate, AnyGrowthRate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecies WithMaxGrowthRate(Func<AnyGrowthRate, AnyGrowthRate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecies copy = new AnySpecies(this);

        copy._maxGrowthRateRecipe = configure(_maxGrowthRateRecipe);
        copy._maxGrowthRate       = copy._maxGrowthRateRecipe;

        return copy;
    }

    /// <summary>Refines the recipe <c>minTemperatureCelsius</c> is drawn from — <c>AnyValidMinTemperatureCelsius()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecies WithMinTemperatureCelsius(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecies copy = new AnySpecies(this);

        copy._minTemperatureCelsiusRecipe = configure(_minTemperatureCelsiusRecipe);
        copy._minTemperatureCelsius       = copy._minTemperatureCelsiusRecipe;

        return copy;
    }

    /// <summary>Refines the recipe <c>optimalTemperatureCelsius</c> is drawn from — <c>AnyValidOptimalTemperatureCelsius()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecies WithOptimalTemperatureCelsius(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecies copy = new AnySpecies(this);

        copy._optimalTemperatureCelsiusRecipe = configure(_optimalTemperatureCelsiusRecipe);
        copy._optimalTemperatureCelsius       = copy._optimalTemperatureCelsiusRecipe;

        return copy;
    }

    /// <summary>Refines the recipe <c>maxTemperatureCelsius</c> is drawn from — <c>AnyValidMaxTemperatureCelsius()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecies WithMaxTemperatureCelsius(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecies copy = new AnySpecies(this);

        copy._maxTemperatureCelsiusRecipe = configure(_maxTemperatureCelsiusRecipe);
        copy._maxTemperatureCelsius       = copy._maxTemperatureCelsiusRecipe;

        return copy;
    }

    /// <summary>Pins <c>lagHours</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpecies WithLagHours(double value) {
        AnySpecies copy = new AnySpecies(this);

        copy._lagHours = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lagHours</c> is drawn from — <c>AnyValidLagHours()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecies WithLagHours(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecies copy = new AnySpecies(this);

        copy._lagHoursRecipe = configure(_lagHoursRecipe);
        copy._lagHours       = copy._lagHoursRecipe;

        return copy;
    }

    /// <summary>Pins <c>yieldCellsPerGram</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpecies WithYieldCellsPerGram(double value) {
        AnySpecies copy = new AnySpecies(this);

        copy._yieldCellsPerGram = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>yieldCellsPerGram</c> is drawn from — <c>AnyValidYieldCellsPerGram()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecies WithYieldCellsPerGram(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecies copy = new AnySpecies(this);

        copy._yieldCellsPerGramRecipe = configure(_yieldCellsPerGramRecipe);
        copy._yieldCellsPerGram       = copy._yieldCellsPerGramRecipe;

        return copy;
    }

    private (double Min, double Optimum, double Max) DrawCardinalTemperatures() {
        (double min, double max) = Any.OrderedPair(_minTemperatureCelsiusRecipe, _maxTemperatureCelsiusRecipe, strict: true).Generate();
        double optimum = _optimalTemperatureCelsiusRecipe.GreaterThan(min).LessThan(max).Generate();

        return (min, optimum, max);
    }

    /// <summary>Produces one arbitrary <see cref="Species" />.</summary>
    public Species Generate() {
        // The cardinal temperatures are tied by min < optimum < max: see DrawCardinalTemperatures, or WithCardinalTemperatures to pin them.
        (double min, double optimum, double max) = DrawCardinalTemperatures();

        return new Species(_binomialName.Generate(),
                           _maxGrowthRate.Generate(),
                           min,
                           optimum,
                           max,
                           _lagHours.Generate(),
                           _yieldCellsPerGram.Generate());
    }

}
