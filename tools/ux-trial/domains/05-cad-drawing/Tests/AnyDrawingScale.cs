// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DrawingScale --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DrawingScale" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDrawingScale : IAny<DrawingScale> {

    private readonly IAny<int> _numerator;
    private readonly IAny<int> _denominator;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDrawingScale()
        : this(numerator:   AnyValidNumerator(),
               denominator: AnyValidDenominator()) { }

    private static AnyInt32 AnyValidNumerator() {
        // ISO 5455 recommended enlargement factors; Generate() keeps 1 on the other side.
        return Any.Int32().OneOf(1, 2, 5, 10, 20, 50);
    }

    private static AnyInt32 AnyValidDenominator() {
        // ISO 5455 recommended enlargement factors; Generate() keeps 1 on the other side.
        // ISO 5455 recommended reduction factors.
        return Any.Int32().OneOf(1, 2, 5, 10, 20, 50, 100, 200, 500, 1000);
    }

    private AnyDrawingScale(IAny<int> numerator,
                            IAny<int> denominator) {
        _numerator   = numerator;
        _denominator = denominator;
    }

    /// <summary>Pins <c>numerator</c> to a fixed value.</summary>
    public AnyDrawingScale WithNumerator(int value) {
        return new AnyDrawingScale(new FixedValue<int>(value), _denominator);
    }

    /// <summary>Draws <c>numerator</c> from <c>AnyValidNumerator()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawingScale WithNumerator(Func<AnyInt32, AnyInt32> configure) {
        return new AnyDrawingScale(configure(AnyValidNumerator()), _denominator);
    }

    /// <summary>Pins <c>denominator</c> to a fixed value.</summary>
    public AnyDrawingScale WithDenominator(int value) {
        return new AnyDrawingScale(_numerator, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>denominator</c> from <c>AnyValidDenominator()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawingScale WithDenominator(Func<AnyInt32, AnyInt32> configure) {
        return new AnyDrawingScale(_numerator, configure(AnyValidDenominator()));
    }

    /// <summary>Produces one arbitrary <see cref="DrawingScale" />.</summary>
    public DrawingScale Generate() {
        // ISO 5455: 1 on one side. The parameters are not independent, so one decides the other.
        int numerator = _numerator.Generate();

        return numerator == 1 ? new DrawingScale(1, _denominator.Generate()) : new DrawingScale(numerator, 1);
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
