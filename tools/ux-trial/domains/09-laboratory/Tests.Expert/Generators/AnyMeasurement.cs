// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Measurement --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Measurement" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasurement : IAny<Measurement> {

    private IAny<double>          _value;
    private IAny<MeasurementUnit> _unit;

    private AnyDouble          _valueRecipe;
    private AnyMeasurementUnit _unitRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasurement()
        : this(value: AnyValidValue(),
               unit:  AnyValidUnit()) { }

    private static AnyDouble AnyValidValue() {
        // A concentration: finite, never negative; bounded where a lab would stop believing the instrument.
        return Any.Double().Between(0d, 100_000d);
    }

    private static AnyMeasurementUnit AnyValidUnit() {
        return new AnyMeasurementUnit();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMeasurement(AnyDouble          value,
                           AnyMeasurementUnit unit) {
        _value       = value;
        _valueRecipe = value;
        _unit        = unit;
        _unitRecipe  = unit;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMeasurement(AnyMeasurement from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
        _unit        = from._unit;
        _unitRecipe  = from._unitRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasurement WithValue(double value) {
        AnyMeasurement copy = new AnyMeasurement(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasurement WithValue(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasurement copy = new AnyMeasurement(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Pins <c>unit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasurement WithUnit(MeasurementUnit value) {
        AnyMeasurement copy = new AnyMeasurement(this);

        copy._unit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unit</c> is drawn from — <c>AnyValidUnit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMeasurementUnit, AnyMeasurementUnit&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasurement WithUnit(Func<AnyMeasurementUnit, AnyMeasurementUnit> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasurement copy = new AnyMeasurement(this);

        copy._unitRecipe = configure(_unitRecipe);
        copy._unit       = copy._unitRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Measurement" />.</summary>
    public Measurement Generate() {
        return new Measurement(_value.Generate(),
                               _unit.Generate());
    }

}
