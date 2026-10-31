// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Race --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Race" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRace : IAny<Race> {

    private readonly IAny<string> _code;
    private readonly IAny<int>    _fieldSize;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRace()
        : this(code:      AnyValidCode(),
               fieldSize: AnyValidFieldSize()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching("[A-Z]{3}-R([1-9]|1[0-6])");
    }

    private static AnyInt32 AnyValidFieldSize() {
        return Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum);
    }

    private AnyRace(IAny<string> code,
                    IAny<int>    fieldSize) {
        _code      = code;
        _fieldSize = fieldSize;
    }

    /// <summary>Pins <c>code</c> to a fixed value.</summary>
    public AnyRace WithCode(string value) {
        return new AnyRace(new FixedValue<string>(value), _fieldSize);
    }

    /// <summary>Draws <c>code</c> from <c>AnyValidCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRace WithCode(Func<AnyPattern, AnyPattern> configure) {
        return new AnyRace(configure(AnyValidCode()), _fieldSize);
    }

    /// <summary>Pins <c>fieldSize</c> to a fixed value.</summary>
    public AnyRace WithFieldSize(int value) {
        return new AnyRace(_code, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>fieldSize</c> from <c>AnyValidFieldSize()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRace WithFieldSize(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRace(_code, configure(AnyValidFieldSize()));
    }

    /// <summary>Produces one arbitrary <see cref="Race" />.</summary>
    public Race Generate() {
        return new Race(_code.Generate(),
                        _fieldSize.Generate());
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
