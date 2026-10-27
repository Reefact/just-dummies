// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DrawingReference --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DrawingReference" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDrawingReference : IAny<DrawingReference> {

    private readonly IAny<string> _number;
    private readonly IAny<char>   _revision;
    private readonly IAny<int>    _sheet;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDrawingReference()
        : this(number:   AnyValidNumber(),
               revision: AnyValidRevision(),
               sheet:    AnyValidSheet()) { }

    private static AnyPattern AnyValidNumber() {
        return Any.StringMatching(@"^(MEC|ELE|HYD)-\d{5}$");
    }

    private static AnyChar AnyValidRevision() {
        // ASME Y14.35 letters: never I, O, Q, S, X or Z.
        return Any.Char().Alpha().InUpperCase().Except('I', 'O', 'Q', 'S', 'X', 'Z');
    }

    private static AnyInt32 AnyValidSheet() {
        return Any.Int32().Positive();
    }

    private AnyDrawingReference(IAny<string> number,
                                IAny<char>   revision,
                                IAny<int>    sheet) {
        _number   = number;
        _revision = revision;
        _sheet    = sheet;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyDrawingReference WithNumber(string value) {
        return new AnyDrawingReference(new FixedValue<string>(value), _revision, _sheet);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawingReference WithNumber(Func<AnyPattern, AnyPattern> configure) {
        return new AnyDrawingReference(configure(AnyValidNumber()), _revision, _sheet);
    }

    /// <summary>Pins <c>revision</c> to a fixed value.</summary>
    public AnyDrawingReference WithRevision(char value) {
        return new AnyDrawingReference(_number, new FixedValue<char>(value), _sheet);
    }

    /// <summary>Draws <c>revision</c> from <c>AnyValidRevision()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawingReference WithRevision(Func<AnyChar, AnyChar> configure) {
        return new AnyDrawingReference(_number, configure(AnyValidRevision()), _sheet);
    }

    /// <summary>Pins <c>sheet</c> to a fixed value.</summary>
    public AnyDrawingReference WithSheet(int value) {
        return new AnyDrawingReference(_number, _revision, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>sheet</c> from <c>AnyValidSheet()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawingReference WithSheet(Func<AnyInt32, AnyInt32> configure) {
        return new AnyDrawingReference(_number, _revision, configure(AnyValidSheet()));
    }

    /// <summary>Produces one arbitrary <see cref="DrawingReference" />.</summary>
    public DrawingReference Generate() {
        return new DrawingReference(_number.Generate(),
                                    _revision.Generate(),
                                    _sheet.Generate());
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
