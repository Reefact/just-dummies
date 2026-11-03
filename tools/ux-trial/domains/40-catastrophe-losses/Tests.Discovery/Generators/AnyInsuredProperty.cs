// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate InsuredProperty --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="InsuredProperty" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyInsuredProperty : IAny<InsuredProperty> {

    private IAny<string>             _id;
    private IAny<GeoPoint>           _location;
    private IAny<decimal>            _insuredValue;
    private IAny<VulnerabilityCurve> _vulnerability;

    private AnyPattern             _idRecipe;
    private AnyGeoPoint           _locationRecipe;
    private AnyDecimal            _insuredValueRecipe;
    private AnyVulnerabilityCurve _vulnerabilityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyInsuredProperty()
        : this(id:            AnyValidId(),
               location:      AnyValidLocation(),
               insuredValue:  AnyValidInsuredValue(),
               vulnerability: AnyValidVulnerability()) { }

    private static AnyPattern AnyValidId() {
        return Any.StringMatching("PRP-[0-9]{6}");
    }

    private static AnyGeoPoint AnyValidLocation() {
        return new AnyGeoPoint();
    }

    private static AnyDecimal AnyValidInsuredValue() {
        return Any.Decimal().Between(1_000m, 50_000_000m).WithScale(2);
    }

    private static AnyVulnerabilityCurve AnyValidVulnerability() {
        return new AnyVulnerabilityCurve();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyInsuredProperty(AnyPattern             id,
                               AnyGeoPoint           location,
                               AnyDecimal            insuredValue,
                               AnyVulnerabilityCurve vulnerability) {
        _id                  = id;
        _idRecipe            = id;
        _location            = location;
        _locationRecipe      = location;
        _insuredValue        = insuredValue;
        _insuredValueRecipe  = insuredValue;
        _vulnerability       = vulnerability;
        _vulnerabilityRecipe = vulnerability;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyInsuredProperty(AnyInsuredProperty from) {
        _id                  = from._id;
        _idRecipe            = from._idRecipe;
        _location            = from._location;
        _locationRecipe      = from._locationRecipe;
        _insuredValue        = from._insuredValue;
        _insuredValueRecipe  = from._insuredValueRecipe;
        _vulnerability       = from._vulnerability;
        _vulnerabilityRecipe = from._vulnerabilityRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInsuredProperty WithId(string value) {
        AnyInsuredProperty copy = new AnyInsuredProperty(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInsuredProperty WithId(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInsuredProperty copy = new AnyInsuredProperty(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>location</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInsuredProperty WithLocation(GeoPoint value) {
        AnyInsuredProperty copy = new AnyInsuredProperty(this);

        copy._location = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>location</c> is drawn from — <c>AnyValidLocation()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGeoPoint, AnyGeoPoint&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInsuredProperty WithLocation(Func<AnyGeoPoint, AnyGeoPoint> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInsuredProperty copy = new AnyInsuredProperty(this);

        copy._locationRecipe = configure(_locationRecipe);
        copy._location       = copy._locationRecipe;

        return copy;
    }

    /// <summary>Pins <c>insuredValue</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInsuredProperty WithInsuredValue(decimal value) {
        AnyInsuredProperty copy = new AnyInsuredProperty(this);

        copy._insuredValue = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>insuredValue</c> is drawn from — <c>AnyValidInsuredValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInsuredProperty WithInsuredValue(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInsuredProperty copy = new AnyInsuredProperty(this);

        copy._insuredValueRecipe = configure(_insuredValueRecipe);
        copy._insuredValue       = copy._insuredValueRecipe;

        return copy;
    }

    /// <summary>Pins <c>vulnerability</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInsuredProperty WithVulnerability(VulnerabilityCurve value) {
        AnyInsuredProperty copy = new AnyInsuredProperty(this);

        copy._vulnerability = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>vulnerability</c> is drawn from — <c>AnyValidVulnerability()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVulnerabilityCurve, AnyVulnerabilityCurve&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInsuredProperty WithVulnerability(Func<AnyVulnerabilityCurve, AnyVulnerabilityCurve> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInsuredProperty copy = new AnyInsuredProperty(this);

        copy._vulnerabilityRecipe = configure(_vulnerabilityRecipe);
        copy._vulnerability       = copy._vulnerabilityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="InsuredProperty" />.</summary>
    public InsuredProperty Generate() {
        return new InsuredProperty(_id.Generate(),
                                   _location.Generate(),
                                   _insuredValue.Generate(),
                                   _vulnerability.Generate());
    }

}
