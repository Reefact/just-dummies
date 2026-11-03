// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Dimension --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Dimension" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDimension : IAny<Dimension> {

    private IAny<double>        _nominal;
    private IAny<LengthUnit>    _unit;
    private IAny<IsoTolerance?> _tolerance;

    private AnyDouble           _nominalRecipe;
    private AnyEnum<LengthUnit> _unitRecipe;
    private AnyIsoTolerance     _toleranceRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDimension()
        : this(nominal:   AnyValidNominal(),
               unit:      AnyValidUnit(),
               tolerance: AnyValidTolerance()) { }

    private static AnyDouble AnyValidNominal() {
        // Valid in either unit: 19.6 in is 497.8 mm, under the 500 mm ceiling Dimension enforces.
        return Any.Double().Between(0.001, 19.6);
    }

    private static AnyEnum<LengthUnit> AnyValidUnit() {
        return Any.Enum<LengthUnit>();
    }

    private static AnyIsoTolerance AnyValidTolerance() {
        return new AnyIsoTolerance();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDimension(AnyDouble           nominal,
                         AnyEnum<LengthUnit> unit,
                         AnyIsoTolerance     tolerance) {
        _nominal         = nominal;
        _nominalRecipe   = nominal;
        _unit            = unit;
        _unitRecipe      = unit;
        _tolerance       = tolerance.OrNull();
        _toleranceRecipe = tolerance;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDimension(AnyDimension from) {
        _nominal         = from._nominal;
        _nominalRecipe   = from._nominalRecipe;
        _unit            = from._unit;
        _unitRecipe      = from._unitRecipe;
        _tolerance       = from._tolerance;
        _toleranceRecipe = from._toleranceRecipe;
    }

    /// <summary>Pins <c>nominal</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDimension WithNominal(double value) {
        AnyDimension copy = new AnyDimension(this);

        copy._nominal = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>nominal</c> is drawn from — <c>AnyValidNominal()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDimension WithNominal(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDimension copy = new AnyDimension(this);

        copy._nominalRecipe = configure(_nominalRecipe);
        copy._nominal       = copy._nominalRecipe;

        return copy;
    }

    /// <summary>Pins <c>unit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDimension WithUnit(LengthUnit value) {
        AnyDimension copy = new AnyDimension(this);

        copy._unit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unit</c> is drawn from — <c>AnyValidUnit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;LengthUnit&gt;, AnyEnum&lt;LengthUnit&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDimension WithUnit(Func<AnyEnum<LengthUnit>, AnyEnum<LengthUnit>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDimension copy = new AnyDimension(this);

        copy._unitRecipe = configure(_unitRecipe);
        copy._unit       = copy._unitRecipe;

        return copy;
    }

    /// <summary>Pins <c>tolerance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDimension WithTolerance(IsoTolerance? value) {
        AnyDimension copy = new AnyDimension(this);

        copy._tolerance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tolerance</c> is drawn from — <c>AnyValidTolerance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIsoTolerance, AnyIsoTolerance&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDimension WithTolerance(Func<AnyIsoTolerance, AnyIsoTolerance> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDimension copy = new AnyDimension(this);

        copy._toleranceRecipe = configure(_toleranceRecipe);
        copy._tolerance       = copy._toleranceRecipe;

        return copy;
    }

    /// <summary>Pins <c>tolerance</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyDimension WithoutTolerance() {
        AnyDimension copy = new AnyDimension(this);

        copy._tolerance = Any.Fixed<IsoTolerance?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Dimension" />.</summary>
    public Dimension Generate() {
        return new Dimension(_nominal.Generate(),
                             _unit.Generate(),
                             _tolerance.Generate());
    }

}
