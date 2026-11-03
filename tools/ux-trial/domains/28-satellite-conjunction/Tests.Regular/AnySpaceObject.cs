// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SpaceObject --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SpaceObject" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpaceObject : IAny<SpaceObject> {

    private IAny<int>        _catalogNumber;
    private IAny<string>     _name;
    private IAny<ObjectKind> _kind;
    private IAny<double>     _hardBodyRadiusMeters;
    private IAny<bool>       _isActive;

    private AnyInt32            _catalogNumberRecipe;
    private AnyString           _nameRecipe;
    private AnyEnum<ObjectKind> _kindRecipe;
    private AnyDouble           _hardBodyRadiusMetersRecipe;
    private AnyBoolean          _isActiveRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpaceObject()
        : this(catalogNumber:        AnyValidCatalogNumber(),
               name:                 AnyValidName(),
               kind:                 AnyValidKind(),
               hardBodyRadiusMeters: AnyValidHardBodyRadiusMeters(),
               isActive:             AnyValidIsActive()) { }

    private static AnyInt32 AnyValidCatalogNumber() {
        return Any.Int32().Positive().LessThanOrEqualTo(SpaceObject.MaximumCatalogNumber);
    }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(SpaceObject.MaximumNameLength);
    }

    private static AnyEnum<ObjectKind> AnyValidKind() {
        return Any.Enum<ObjectKind>();
    }

    private static AnyDouble AnyValidHardBodyRadiusMeters() {
        return Any.Double().Positive().LessThanOrEqualTo(SpaceObject.MaximumHardBodyRadiusMeters);
    }

    private static AnyBoolean AnyValidIsActive() {
        // Only a payload may be active: Generate() draws the kind first and keeps this flag only for a payload.
        return Any.Boolean();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySpaceObject(AnyInt32            catalogNumber,
                           AnyString           name,
                           AnyEnum<ObjectKind> kind,
                           AnyDouble           hardBodyRadiusMeters,
                           AnyBoolean          isActive) {
        _catalogNumber              = catalogNumber;
        _catalogNumberRecipe        = catalogNumber;
        _name                       = name;
        _nameRecipe                 = name;
        _kind                       = kind;
        _kindRecipe                 = kind;
        _hardBodyRadiusMeters       = hardBodyRadiusMeters;
        _hardBodyRadiusMetersRecipe = hardBodyRadiusMeters;
        _isActive                   = isActive;
        _isActiveRecipe             = isActive;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySpaceObject(AnySpaceObject from) {
        _catalogNumber              = from._catalogNumber;
        _catalogNumberRecipe        = from._catalogNumberRecipe;
        _name                       = from._name;
        _nameRecipe                 = from._nameRecipe;
        _kind                       = from._kind;
        _kindRecipe                 = from._kindRecipe;
        _hardBodyRadiusMeters       = from._hardBodyRadiusMeters;
        _hardBodyRadiusMetersRecipe = from._hardBodyRadiusMetersRecipe;
        _isActive                   = from._isActive;
        _isActiveRecipe             = from._isActiveRecipe;
    }

    /// <summary>Pins <c>catalogNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpaceObject WithCatalogNumber(int value) {
        AnySpaceObject copy = new AnySpaceObject(this);

        copy._catalogNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>catalogNumber</c> is drawn from — <c>AnyValidCatalogNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpaceObject WithCatalogNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpaceObject copy = new AnySpaceObject(this);

        copy._catalogNumberRecipe = configure(_catalogNumberRecipe);
        copy._catalogNumber       = copy._catalogNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpaceObject WithName(string value) {
        AnySpaceObject copy = new AnySpaceObject(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpaceObject WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpaceObject copy = new AnySpaceObject(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpaceObject WithKind(ObjectKind value) {
        AnySpaceObject copy = new AnySpaceObject(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ObjectKind&gt;, AnyEnum&lt;ObjectKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpaceObject WithKind(Func<AnyEnum<ObjectKind>, AnyEnum<ObjectKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpaceObject copy = new AnySpaceObject(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>hardBodyRadiusMeters</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpaceObject WithHardBodyRadiusMeters(double value) {
        AnySpaceObject copy = new AnySpaceObject(this);

        copy._hardBodyRadiusMeters = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>hardBodyRadiusMeters</c> is drawn from — <c>AnyValidHardBodyRadiusMeters()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpaceObject WithHardBodyRadiusMeters(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpaceObject copy = new AnySpaceObject(this);

        copy._hardBodyRadiusMetersRecipe = configure(_hardBodyRadiusMetersRecipe);
        copy._hardBodyRadiusMeters       = copy._hardBodyRadiusMetersRecipe;

        return copy;
    }

    /// <summary>Pins <c>isActive</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpaceObject WithIsActive(bool value) {
        AnySpaceObject copy = new AnySpaceObject(this);

        copy._isActive = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>isActive</c> is drawn from — <c>AnyValidIsActive()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpaceObject WithIsActive(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpaceObject copy = new AnySpaceObject(this);

        copy._isActiveRecipe = configure(_isActiveRecipe);
        copy._isActive       = copy._isActiveRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SpaceObject" />.</summary>
    public SpaceObject Generate() {
        ObjectKind kind = _kind.Generate();
        bool       isActive = _isActive.Generate() && kind == ObjectKind.Payload;

        return new SpaceObject(_catalogNumber.Generate(),
                               _name.Generate(),
                               kind,
                               _hardBodyRadiusMeters.Generate(),
                               isActive);
    }

}
