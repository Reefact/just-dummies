// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TemperatureRange --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Warehouse.Domain;

namespace Warehouse.Domain.Tests.Generators;

/// <summary>
///     A generator of arbitrary <see cref="TemperatureRange" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTemperatureRange : IAny<TemperatureRange> {

    private AnyOrderedPair<decimal> _minCelsiusAndMaxCelsius;

    private AnyDecimal _minCelsiusRecipe;
    private AnyDecimal _maxCelsiusRecipe;

    private decimal? _minCelsiusPin;
    private decimal? _maxCelsiusPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTemperatureRange()
        : this(minCelsius: AnyValidMinCelsius(),
               maxCelsius: AnyValidMaxCelsius()) { }

    private static AnyDecimal AnyValidMinCelsius() {
        return Any.Decimal().GreaterThanOrEqualTo(-40m);
    }

    private static AnyDecimal AnyValidMaxCelsius() {
        return Any.Decimal().LessThanOrEqualTo(40m);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTemperatureRange(AnyDecimal minCelsius,
                                AnyDecimal maxCelsius) {
        _minCelsiusRecipe        = minCelsius;
        _maxCelsiusRecipe        = maxCelsius;
        _minCelsiusAndMaxCelsius = MinCelsiusAndMaxCelsius();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTemperatureRange(AnyTemperatureRange from) {
        _minCelsiusAndMaxCelsius = from._minCelsiusAndMaxCelsius;
        _minCelsiusRecipe        = from._minCelsiusRecipe;
        _minCelsiusPin           = from._minCelsiusPin;
        _maxCelsiusRecipe        = from._maxCelsiusRecipe;
        _maxCelsiusPin           = from._maxCelsiusPin;
    }

    /// <summary>Draws <c>minCelsius</c> and <c>maxCelsius</c> together, <c>minCelsius</c> always before <c>maxCelsius</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<decimal> MinCelsiusAndMaxCelsius() {
        AnyOrderedPair<decimal> pair = Any.OrderedPair(_minCelsiusRecipe, _maxCelsiusRecipe, strict: true);

        if (_minCelsiusPin is { } minCelsius) { pair = pair.WithFirst(minCelsius); }
        if (_maxCelsiusPin is { } maxCelsius) { pair = pair.WithSecond(maxCelsius); }

        return pair;
    }

    /// <summary>Pins <c>minCelsius</c> to a fixed value, which bounds <c>maxCelsius</c>; its recipe is kept for a later refinement.</summary>
    public AnyTemperatureRange WithMinCelsius(decimal value) {
        AnyTemperatureRange copy = new AnyTemperatureRange(this);

        copy._minCelsiusPin           = value;
        copy._minCelsiusAndMaxCelsius = copy.MinCelsiusAndMaxCelsius();

        return copy;
    }

    /// <summary>Refines the recipe <c>minCelsius</c> is drawn from — <c>AnyValidMinCelsius()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>maxCelsius</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTemperatureRange WithMinCelsius(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTemperatureRange copy = new AnyTemperatureRange(this);

        copy._minCelsiusRecipe        = configure(_minCelsiusRecipe);
        copy._minCelsiusPin           = null;
        copy._minCelsiusAndMaxCelsius = copy.MinCelsiusAndMaxCelsius();

        return copy;
    }

    /// <summary>Pins <c>maxCelsius</c> to a fixed value, which bounds <c>minCelsius</c>; its recipe is kept for a later refinement.</summary>
    public AnyTemperatureRange WithMaxCelsius(decimal value) {
        AnyTemperatureRange copy = new AnyTemperatureRange(this);

        copy._maxCelsiusPin           = value;
        copy._minCelsiusAndMaxCelsius = copy.MinCelsiusAndMaxCelsius();

        return copy;
    }

    /// <summary>Refines the recipe <c>maxCelsius</c> is drawn from — <c>AnyValidMaxCelsius()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>minCelsius</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTemperatureRange WithMaxCelsius(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTemperatureRange copy = new AnyTemperatureRange(this);

        copy._maxCelsiusRecipe        = configure(_maxCelsiusRecipe);
        copy._maxCelsiusPin           = null;
        copy._minCelsiusAndMaxCelsius = copy.MinCelsiusAndMaxCelsius();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TemperatureRange" />.</summary>
    public TemperatureRange Generate() {
        (decimal minCelsius, decimal maxCelsius) = _minCelsiusAndMaxCelsius.Generate();

        return new TemperatureRange(minCelsius,
                                    maxCelsius);
    }

}
