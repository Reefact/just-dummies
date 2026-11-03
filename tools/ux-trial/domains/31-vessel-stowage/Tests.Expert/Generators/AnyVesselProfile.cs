// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VesselProfile --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using VesselStowage.Domain;

namespace VesselStowage.Domain.Tests.Generators;

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
    private AnyOrderedPair<decimal> _lightshipTonnesAndMaxDisplacementTonnes;
    private AnyOrderedPair<double>  _lightshipVcgMetresAndMaxVcgMetres;
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

    private double?  _lightshipVcgMetresPin;
    private double?  _maxVcgMetresPin;
    private decimal? _lightshipTonnesPin;
    private decimal? _maxDisplacementTonnesPin;

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
        return Any.String().NotBlank().WithLengthBetween(3, 30);
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
        return Any.Decimal().Between(1m, 100_000m);
    }

    private static AnyDecimal AnyValidLightshipTonnes() {
        return Any.Decimal().Between(100m, 500_000m);
    }

    private static AnyDouble AnyValidLightshipVcgMetres() {
        return Any.Double().Between(0.1, 50.0);
    }

    private static AnyDecimal AnyValidMaxDisplacementTonnes() {
        return Any.Decimal().Between(200m, 50_000_000m);
    }

    private static AnyDouble AnyValidMaxVcgMetres() {
        return Any.Double().Between(1.0, 2_000.0);
    }

    private static AnyDouble AnyValidMaxTcgMetres() {
        return Any.Double().Between(0.01, 200.0);
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
        _lightshipTonnesAndMaxDisplacementTonnes = LightshipTonnesAndMaxDisplacementTonnes();
        _lightshipVcgMetresAndMaxVcgMetres       = LightshipVcgMetresAndMaxVcgMetres();
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
        _lightshipTonnesAndMaxDisplacementTonnes = from._lightshipTonnesAndMaxDisplacementTonnes;
        _lightshipTonnesRecipe                   = from._lightshipTonnesRecipe;
        _lightshipTonnesPin                      = from._lightshipTonnesPin;
        _lightshipVcgMetresAndMaxVcgMetres       = from._lightshipVcgMetresAndMaxVcgMetres;
        _lightshipVcgMetresPin                   = from._lightshipVcgMetresPin;
        _lightshipVcgMetresRecipe                = from._lightshipVcgMetresRecipe;
        _maxDisplacementTonnesRecipe             = from._maxDisplacementTonnesRecipe;
        _maxDisplacementTonnesPin                = from._maxDisplacementTonnesPin;
        _maxVcgMetresPin                         = from._maxVcgMetresPin;
        _maxVcgMetresRecipe                      = from._maxVcgMetresRecipe;
        _maxTcgMetres                            = from._maxTcgMetres;
        _maxTcgMetresRecipe                      = from._maxTcgMetresRecipe;
    }

    /// <summary>Draws <c>lightshipTonnes</c> and <c>maxDisplacementTonnes</c> together, <c>lightshipTonnes</c> always before <c>maxDisplacementTonnes</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<decimal> LightshipTonnesAndMaxDisplacementTonnes() {
        AnyOrderedPair<decimal> pair = Any.OrderedPair(_lightshipTonnesRecipe, _maxDisplacementTonnesRecipe, strict: true);

        if (_lightshipTonnesPin is { } lightshipTonnes) { pair = pair.WithFirst(lightshipTonnes); }
        if (_maxDisplacementTonnesPin is { } maxDisplacementTonnes) { pair = pair.WithSecond(maxDisplacementTonnes); }

        return pair;
    }

    /// <summary>Draws <c>lightshipVcgMetres</c> and <c>maxVcgMetres</c> together, the maximum always above the lightship height as the constructor requires.</summary>
    private AnyOrderedPair<double> LightshipVcgMetresAndMaxVcgMetres() {
        AnyOrderedPair<double> pair = Any.OrderedPair(_lightshipVcgMetresRecipe, _maxVcgMetresRecipe, strict: true);

        if (_lightshipVcgMetresPin is { } lightshipVcgMetres) { pair = pair.WithFirst(lightshipVcgMetres); }
        if (_maxVcgMetresPin is { } maxVcgMetres) { pair = pair.WithSecond(maxVcgMetres); }

        return pair;
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
        copy._lightshipTonnesAndMaxDisplacementTonnes = copy.LightshipTonnesAndMaxDisplacementTonnes();

        return copy;
    }

    /// <summary>Refines the recipe <c>lightshipTonnes</c> is drawn from — <c>AnyValidLightshipTonnes()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>maxDisplacementTonnes</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithLightshipTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._lightshipTonnesRecipe                   = configure(_lightshipTonnesRecipe);
        copy._lightshipTonnesPin                      = null;
        copy._lightshipTonnesAndMaxDisplacementTonnes = copy.LightshipTonnesAndMaxDisplacementTonnes();

        return copy;
    }

    /// <summary>Pins <c>lightshipVcgMetres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithLightshipVcgMetres(double value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._lightshipVcgMetresPin            = value;
        copy._lightshipVcgMetresAndMaxVcgMetres = copy.LightshipVcgMetresAndMaxVcgMetres();

        return copy;
    }

    /// <summary>Refines the recipe <c>lightshipVcgMetres</c> is drawn from — <c>AnyValidLightshipVcgMetres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithLightshipVcgMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._lightshipVcgMetresRecipe          = configure(_lightshipVcgMetresRecipe);
        copy._lightshipVcgMetresPin             = null;
        copy._lightshipVcgMetresAndMaxVcgMetres = copy.LightshipVcgMetresAndMaxVcgMetres();

        return copy;
    }

    /// <summary>Pins <c>maxDisplacementTonnes</c> to a fixed value, which bounds <c>lightshipTonnes</c>; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithMaxDisplacementTonnes(decimal value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxDisplacementTonnesPin                = value;
        copy._lightshipTonnesAndMaxDisplacementTonnes = copy.LightshipTonnesAndMaxDisplacementTonnes();

        return copy;
    }

    /// <summary>Refines the recipe <c>maxDisplacementTonnes</c> is drawn from — <c>AnyValidMaxDisplacementTonnes()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>lightshipTonnes</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithMaxDisplacementTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxDisplacementTonnesRecipe             = configure(_maxDisplacementTonnesRecipe);
        copy._maxDisplacementTonnesPin                = null;
        copy._lightshipTonnesAndMaxDisplacementTonnes = copy.LightshipTonnesAndMaxDisplacementTonnes();

        return copy;
    }

    /// <summary>Pins <c>maxVcgMetres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVesselProfile WithMaxVcgMetres(double value) {
        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxVcgMetresPin                   = value;
        copy._lightshipVcgMetresAndMaxVcgMetres = copy.LightshipVcgMetresAndMaxVcgMetres();

        return copy;
    }

    /// <summary>Refines the recipe <c>maxVcgMetres</c> is drawn from — <c>AnyValidMaxVcgMetres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVesselProfile WithMaxVcgMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVesselProfile copy = new AnyVesselProfile(this);

        copy._maxVcgMetresRecipe                = configure(_maxVcgMetresRecipe);
        copy._maxVcgMetresPin                   = null;
        copy._lightshipVcgMetresAndMaxVcgMetres = copy.LightshipVcgMetresAndMaxVcgMetres();

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
        (decimal lightshipTonnes, decimal maxDisplacementTonnes) = _lightshipTonnesAndMaxDisplacementTonnes.Generate();
        (double lightshipVcgMetres, double maxVcgMetres)         = _lightshipVcgMetresAndMaxVcgMetres.Generate();

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
