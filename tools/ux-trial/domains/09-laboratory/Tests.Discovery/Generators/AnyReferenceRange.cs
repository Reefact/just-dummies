// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReferenceRange --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReferenceRange" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReferenceRange : IAny<ReferenceRange> {

    private IAny<MeasurementUnit>  _unit;
    private IAny<double?>          _criticalLow;
    private IAny<double?>          _criticalHigh;

    private AnyDouble          _lowerRecipe;
    private AnyDouble          _upperRecipe;
    private AnyMeasurementUnit _unitRecipe;
    private AnyDouble          _criticalLowRecipe;
    private AnyDouble          _criticalHighRecipe;

    private double? _lowerPin;
    private double? _upperPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReferenceRange()
        : this(lower:        AnyValidLower(),
               upper:        AnyValidUpper(),
               unit:         AnyValidUnit(),
               criticalLow:  AnyValidCriticalLow(),
               criticalHigh: AnyValidCriticalHigh()) { }

    private static AnyDouble AnyValidLower() {
        // Four bands that meet but never cross: criticalLow <= lower < upper <= criticalHigh holds by construction.
        return Any.Double().Between(0d, 500d);
    }

    private static AnyDouble AnyValidUpper() {
        return Any.Double().Between(500d, 1000d);
    }

    private static AnyMeasurementUnit AnyValidUnit() {
        return new AnyMeasurementUnit();
    }

    private static AnyDouble AnyValidCriticalLow() {
        return Any.Double().Between(-50d, 0d);
    }

    private static AnyDouble AnyValidCriticalHigh() {
        return Any.Double().Between(1000d, 2000d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReferenceRange(AnyDouble          lower,
                              AnyDouble          upper,
                              AnyMeasurementUnit unit,
                              AnyDouble          criticalLow,
                              AnyDouble          criticalHigh) {
        _lowerRecipe        = lower;
        _upperRecipe        = upper;
        _unit               = unit;
        _unitRecipe         = unit;
        _criticalLow        = criticalLow.OrNull();
        _criticalLowRecipe  = criticalLow;
        _criticalHigh       = criticalHigh.OrNull();
        _criticalHighRecipe = criticalHigh;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReferenceRange(AnyReferenceRange from) {
        _lowerRecipe        = from._lowerRecipe;
        _lowerPin           = from._lowerPin;
        _upperRecipe        = from._upperRecipe;
        _upperPin           = from._upperPin;
        _unit               = from._unit;
        _unitRecipe         = from._unitRecipe;
        _criticalLow        = from._criticalLow;
        _criticalLowRecipe  = from._criticalLowRecipe;
        _criticalHigh       = from._criticalHigh;
        _criticalHighRecipe = from._criticalHighRecipe;
    }

    /// <summary>Draws <c>lower</c> and <c>upper</c>, <c>lower</c> always before <c>upper</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (double Lower, double Upper) LowerAndUpper() {
        if (_lowerPin is { } lower) { return (lower, _upperPin ?? _upperRecipe.GreaterThan(lower).Generate()); }
        if (_upperPin is { } upper) { return (_lowerRecipe.LessThan(upper).Generate(), upper); }

        return Any.Pair(_lowerRecipe, _upperRecipe).Having.Item1LessThanItem2().Generate();
    }

    /// <summary>Pins <c>lower</c> to a fixed value, which bounds <c>upper</c>; its recipe is kept for a later refinement.</summary>
    public AnyReferenceRange WithLower(double value) {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._lowerPin      = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>lower</c> is drawn from — <c>AnyValidLower()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>upper</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReferenceRange WithLower(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._lowerRecipe   = configure(_lowerRecipe);
        copy._lowerPin      = null;

        return copy;
    }

    /// <summary>Pins <c>upper</c> to a fixed value, which bounds <c>lower</c>; its recipe is kept for a later refinement.</summary>
    public AnyReferenceRange WithUpper(double value) {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._upperPin      = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>upper</c> is drawn from — <c>AnyValidUpper()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>lower</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReferenceRange WithUpper(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._upperRecipe   = configure(_upperRecipe);
        copy._upperPin      = null;

        return copy;
    }

    /// <summary>Pins <c>unit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReferenceRange WithUnit(MeasurementUnit value) {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._unit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unit</c> is drawn from — <c>AnyValidUnit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMeasurementUnit, AnyMeasurementUnit&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReferenceRange WithUnit(Func<AnyMeasurementUnit, AnyMeasurementUnit> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._unitRecipe = configure(_unitRecipe);
        copy._unit       = copy._unitRecipe;

        return copy;
    }

    /// <summary>Pins <c>criticalLow</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReferenceRange WithCriticalLow(double? value) {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._criticalLow = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>criticalLow</c> is drawn from — <c>AnyValidCriticalLow()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReferenceRange WithCriticalLow(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._criticalLowRecipe = configure(_criticalLowRecipe);
        copy._criticalLow       = copy._criticalLowRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>criticalLow</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyReferenceRange WithoutCriticalLow() {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._criticalLow = Any.Fixed<double?>(null);

        return copy;
    }

    /// <summary>Pins <c>criticalHigh</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReferenceRange WithCriticalHigh(double? value) {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._criticalHigh = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>criticalHigh</c> is drawn from — <c>AnyValidCriticalHigh()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReferenceRange WithCriticalHigh(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._criticalHighRecipe = configure(_criticalHighRecipe);
        copy._criticalHigh       = copy._criticalHighRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>criticalHigh</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyReferenceRange WithoutCriticalHigh() {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._criticalHigh = Any.Fixed<double?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReferenceRange" />.</summary>
    public ReferenceRange Generate() {
        (double lower, double upper) = LowerAndUpper();

        return new ReferenceRange(lower,
                                  upper,
                                  _unit.Generate(),
                                  _criticalLow.Generate(),
                                  _criticalHigh.Generate());
    }

}
