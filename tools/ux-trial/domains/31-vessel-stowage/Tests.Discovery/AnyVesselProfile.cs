// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VesselProfile --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VesselProfile" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVesselProfile : IAny<VesselProfile> {

    private IAny<string>            _name;
    private IAny<int>               _bayCount;
    private IAny<int>               _rowCount;
    private IAny<int>               _holdTiers;
    private IAny<int>               _deckTiers;
    private IAny<decimal>           _maxStackWeightTonnes;
    private IAny<double>            _maxTcgMetres;

    private AnyString  _nameRecipe;
    private AnyInt32   _bayCountRecipe;
    private AnyInt32   _rowCountRecipe;
    private AnyInt32   _holdTiersRecipe;
    private AnyInt32   _deckTiersRecipe;
    private AnyDecimal _maxStackWeightTonnesRecipe;
    private AnyDecimal _lightshipTonnesRecipe;
    private AnyDouble  _lightshipVcgMetresRecipe;
    private AnyDecimal _maxDisplacementTonnesRecipe;
    private AnyDouble  _maxVcgMetresRecipe;
    private AnyDouble  _maxTcgMetresRecipe;

    private decimal? _lightshipTonnesPin;
    private decimal? _maxDisplacementTonnesPin;
    private double?  _lightshipVcgMetresPin;
    private double?  _maxVcgMetresPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVesselProfile()
        : this(name:                  AnyValidName(),
               bayCount:              AnyValidBayCount(),
               rowCount:              AnyValidRowCount(),
               holdTiers:             AnyValidHoldTiers(),
               deckTiers:             AnyValidDeckTiers(),
               maxStackWeightTonnes:  AnyValidMaxStackWeightTonnes(),
               lightshipTonnes:       AnyValidLightshipTonnes(),
               lightshipVcgMetres:    AnyValidLightshipVcgMetres(),
               maxDisplacementTonnes: AnyValidMaxDisplacementTonnes(),
               maxVcgMetres:          AnyValidMaxVcgMetres(),
               maxTcgMetres:          AnyValidMaxTcgMetres()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithLengthBetween(1, 30);
    }

    private static AnyInt32 AnyValidBayCount() {
        return Any.Int32().Positive().LessThanOrEqualTo(99);
    }

    private static AnyInt32 AnyValidRowCount() {
        return Any.Int32().Positive().LessThanOrEqualTo(21);
    }

    private static AnyInt32 AnyValidHoldTiers() {
        return Any.Int32().Positive().LessThanOrEqualTo(10);
    }

    private static AnyInt32 AnyValidDeckTiers() {
        return Any.Int32().Between(0, 9);
    }

    private static AnyDecimal AnyValidMaxStackWeightTonnes() {
        return Any.Decimal().Between(60m, 200m).WithScale(1);
    }

    private static AnyDecimal AnyValidLightshipTonnes() {
        return Any.Decimal().Between(5_000m, 60_000m).WithScale(0);
    }

    private static AnyDouble AnyValidLightshipVcgMetres() {
        return Any.Double().Between(5.0, 20.0);
    }

    private static AnyDecimal AnyValidMaxDisplacementTonnes() {
        return Any.Decimal().Between(10_000m, 200_000m).WithScale(0);
    }

    private static AnyDouble AnyValidMaxVcgMetres() {
        return Any.Double().Between(10.0, 40.0);
    }

    private static AnyDouble AnyValidMaxTcgMetres() {
        return Any.Double().Between(0.5, 5.0);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVesselProfile(AnyString  name,
                             AnyInt32   bayCount,
                             AnyInt32   rowCount,
                             AnyInt32   holdTiers,
                             AnyInt32   deckTiers,
                             AnyDecimal maxStackWeightTonnes,
                             AnyDecimal lightshipTonnes,
                             AnyDouble  lightshipVcgMetres,
                             AnyDecimal maxDisplacementTonnes,
                             AnyDouble  maxVcgMetres,
                             AnyDouble  maxTcgMetres) {
        _name                                    = name;
        _nameRecipe                              = name;
        _bayCount                                = bayCount;
        _bayCountRecipe                          = bayCount;
        _rowCount                                = rowCount;
        _rowCountRecipe                          = rowCount;
        _holdTiers                               = holdTiers;
        _holdTiersRecipe                         = holdTiers;
        _deckTiers                               = deckTiers;
        _deckTiersRecipe                         = deckTiers;
        _maxStackWeightTonnes                    = maxStackWeightTonnes;
        _maxStackWeightTonnesRecipe              = maxStackWeightTonnes;
        _lightshipTonnesRecipe                   = lightshipTonnes;
        _lightshipVcgMetresRecipe                = lightshipVcgMetres;
        _maxDisplacementTonnesRecipe             = maxDisplacementTonnes;
        _maxVcgMetresRecipe                      = maxVcgMetres;
        _maxTcgMetres                            = maxTcgMetres;
        _maxTcgMetresRecipe                      = maxTcgMetres;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVesselProfile(AnyVesselProfile from) {
        _name                                    = from._name;
        _nameRecipe                              = from._nameRecipe;
        _bayCount                                = from._bayCount;
        _bayCountRecipe                          = from._bayCountRecipe;
        _rowCount                                = from._rowCount;
        _rowCountRecipe                          = from._rowCountRecipe;
        _holdTiers                               = from._holdTiers;
        _holdTiersRecipe                         = from._holdTiersRecipe;
        _deckTiers                               = from._deckTiers;
        _deckTiersRecipe                         = from._deckTiersRecipe;
        _maxStackWeightTonnes                    = from._maxStackWeightTonnes;
        _maxStackWeightTonnesRecipe              = from._maxStackWeightTonnesRecipe;
        _lightshipTonnesRecipe                   = from._lightshipTonnesRecipe;
        _lightshipTonnesPin                      = from._lightshipTonnesPin;
        _lightshipVcgMetresPin                   = from._lightshipVcgMetresPin;
        _lightshipVcgMetresRecipe                = from._lightshipVcgMetresRecipe;
        _maxDisplacementTonnesRecipe             = from._maxDisplacementTonnesRecipe;
        _maxDisplacementTonnesPin                = from._maxDisplacementTonnesPin;
        _maxVcgMetresPin                         = from._maxVcgMetresPin;
        _maxVcgMetresRecipe                      = from._maxVcgMetresRecipe;
        _maxTcgMetres                            = from._maxTcgMetres;
        _maxTcgMetresRecipe                      = from._maxTcgMetresRecipe;
    }

    /// <summary>Draws <c>lightshipTonnes</c> and <c>maxDisplacementTonnes</c>, <c>lightshipTonnes</c> always before <c>maxDisplacementTonnes</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (decimal LightshipTonnes, decimal MaxDisplacementTonnes) LightshipTonnesAndMaxDisplacementTonnes() {
        if (_lightshipTonnesPin is { } lightshipTonnes) { return (lightshipTonnes, _maxDisplacementTonnesPin ?? _maxDisplacementTonnesRecipe.GreaterThan(lightshipTonnes).Generate()); }
        if (_maxDisplacementTonnesPin is { } maxDisplacementTonnes) { return (_lightshipTonnesRecipe.LessThan(maxDisplacementTonnes).Generate(), maxDisplacementTonnes); }

        return Any.Pair(_lightshipTonnesRecipe, _maxDisplacementTonnesRecipe).Having.Item1LessThanItem2().Generate();
    }

    /// <summary>Draws <c>lightshipVcgMetres</c> and <c>maxVcgMetres</c>, <c>lightshipVcgMetres</c> always before <c>maxVcgMetres</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (double LightshipVcgMetres, double MaxVcgMetres) LightshipVcgMetresAndMaxVcgMetres() {
        if (_lightshipVcgMetresPin is { } lightshipVcgMetres) { return (lightshipVcgMetres, _maxVcgMetresPin ?? _maxVcgMetresRecipe.GreaterThan(lightshipVcgMetres).Generate()); }
        if (_maxVcgMetresPin is { } maxVcgMetres) { return (_lightshipVcgMetresRecipe.LessThan(maxVcgMetres).Generate(), maxVcgMetres); }

        return Any.Pair(_lightshipVcgMetresRecipe, _maxVcgMetresRecipe).Having.Item1LessThanItem2().Generate();
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithName(string value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>bayCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithBayCount(int value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._bayCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bayCount</c> is drawn from — <c>AnyValidBayCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithBayCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._bayCountRecipe = configure(_bayCountRecipe);
        copy._bayCount       = copy._bayCountRecipe;

        return copy;
    }

    /// <summary>Pins <c>rowCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithRowCount(int value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._rowCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rowCount</c> is drawn from — <c>AnyValidRowCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithRowCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._rowCountRecipe = configure(_rowCountRecipe);
        copy._rowCount       = copy._rowCountRecipe;

        return copy;
    }

    /// <summary>Pins <c>holdTiers</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithHoldTiers(int value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._holdTiers = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>holdTiers</c> is drawn from — <c>AnyValidHoldTiers()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithHoldTiers(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._holdTiersRecipe = configure(_holdTiersRecipe);
        copy._holdTiers       = copy._holdTiersRecipe;

        return copy;
    }

    /// <summary>Pins <c>deckTiers</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithDeckTiers(int value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._deckTiers = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deckTiers</c> is drawn from — <c>AnyValidDeckTiers()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithDeckTiers(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._deckTiersRecipe = configure(_deckTiersRecipe);
        copy._deckTiers       = copy._deckTiersRecipe;

        return copy;
    }

    /// <summary>Pins <c>maxStackWeightTonnes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithMaxStackWeightTonnes(decimal value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxStackWeightTonnes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maxStackWeightTonnes</c> is drawn from — <c>AnyValidMaxStackWeightTonnes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithMaxStackWeightTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxStackWeightTonnesRecipe = configure(_maxStackWeightTonnesRecipe);
        copy._maxStackWeightTonnes       = copy._maxStackWeightTonnesRecipe;

        return copy;
    }

    /// <summary>Pins <c>lightshipTonnes</c> to a fixed value, which bounds <c>maxDisplacementTonnes</c>; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithLightshipTonnes(decimal value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._lightshipTonnesPin                      = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>lightshipTonnes</c> is drawn from — <c>AnyValidLightshipTonnes()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>maxDisplacementTonnes</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithLightshipTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._lightshipTonnesRecipe                   = configure(_lightshipTonnesRecipe);
        copy._lightshipTonnesPin                      = null;

        return copy;
    }

    /// <summary>Pins <c>lightshipVcgMetres</c> to a fixed value, which bounds <c>maxVcgMetres</c>; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithLightshipVcgMetres(double value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._lightshipVcgMetresPin                = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>lightshipVcgMetres</c> is drawn from, in order with <c>maxVcgMetres</c>.</summary>
    public AnyVesselProfile WithLightshipVcgMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._lightshipVcgMetresRecipe             = configure(_lightshipVcgMetresRecipe);
        copy._lightshipVcgMetresPin                = null;

        return copy;
    }

    /// <summary>Pins <c>maxDisplacementTonnes</c> to a fixed value, which bounds <c>lightshipTonnes</c>; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithMaxDisplacementTonnes(decimal value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxDisplacementTonnesPin                = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>maxDisplacementTonnes</c> is drawn from — <c>AnyValidMaxDisplacementTonnes()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>lightshipTonnes</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithMaxDisplacementTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxDisplacementTonnesRecipe             = configure(_maxDisplacementTonnesRecipe);
        copy._maxDisplacementTonnesPin                = null;

        return copy;
    }

    /// <summary>Pins <c>maxVcgMetres</c> to a fixed value, which bounds <c>lightshipVcgMetres</c>; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithMaxVcgMetres(double value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxVcgMetresPin                      = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>maxVcgMetres</c> is drawn from, in order with <c>lightshipVcgMetres</c>.</summary>
    public AnyVesselProfile WithMaxVcgMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxVcgMetresRecipe                   = configure(_maxVcgMetresRecipe);
        copy._maxVcgMetresPin                      = null;

        return copy;
    }

    /// <summary>Pins <c>maxTcgMetres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithMaxTcgMetres(double value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxTcgMetres = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maxTcgMetres</c> is drawn from — <c>AnyValidMaxTcgMetres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithMaxTcgMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxTcgMetresRecipe = configure(_maxTcgMetresRecipe);
        copy._maxTcgMetres       = copy._maxTcgMetresRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VesselProfile" />.</summary>
    public VesselProfile Generate() {
        (decimal lightshipTonnes, decimal maxDisplacementTonnes) = LightshipTonnesAndMaxDisplacementTonnes();
        (double lightshipVcgMetres, double maxVcgMetres)         = LightshipVcgMetresAndMaxVcgMetres();

        return new VesselProfile(_name.Generate(),
                                 _bayCount.Generate(),
                                 _rowCount.Generate(),
                                 _holdTiers.Generate(),
                                 _deckTiers.Generate(),
                                 _maxStackWeightTonnes.Generate(),
                                 lightshipTonnes,
                                 lightshipVcgMetres,
                                 maxDisplacementTonnes,
                                 maxVcgMetres,
                                 _maxTcgMetres.Generate());
    }

}
