// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Culture --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Culture" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCulture : IAny<Culture> {

    private readonly IAny<CultureId>              _id;
    private readonly IAny<ExperimentalConditions> _conditions;
    private readonly IAny<DateTimeOffset>         _startedAtUtc;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCulture()
        : this(id:           AnyValidId(),
               conditions:   AnyValidConditions(),
               startedAtUtc: AnyValidStartedAtUtc()) { }

    private static AnyCultureId AnyValidId() {
        return new AnyCultureId();
    }

    private static AnyExperimentalConditions AnyValidConditions() {
        return new AnyExperimentalConditions();
    }

    private static AnyDateTimeOffset AnyValidStartedAtUtc() {
        // Station time is UTC: the offset stays +00:00.
        return Any.DateTimeOffset()
                  .WithOffset(TimeSpan.Zero)
                  .Between(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private AnyCulture(IAny<CultureId>              id,
                       IAny<ExperimentalConditions> conditions,
                       IAny<DateTimeOffset>         startedAtUtc) {
        _id           = id;
        _conditions   = conditions;
        _startedAtUtc = startedAtUtc;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyCulture WithId(CultureId value) {
        return new AnyCulture(new FixedValue<CultureId>(value), _conditions, _startedAtUtc);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCulture WithId(Func<AnyCultureId, AnyCultureId> configure) {
        return new AnyCulture(configure(AnyValidId()), _conditions, _startedAtUtc);
    }

    /// <summary>Pins <c>conditions</c> to a fixed value.</summary>
    public AnyCulture WithConditions(ExperimentalConditions value) {
        return new AnyCulture(_id, new FixedValue<ExperimentalConditions>(value), _startedAtUtc);
    }

    /// <summary>Draws <c>conditions</c> from <c>AnyValidConditions()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCulture WithConditions(Func<AnyExperimentalConditions, AnyExperimentalConditions> configure) {
        return new AnyCulture(_id, configure(AnyValidConditions()), _startedAtUtc);
    }

    /// <summary>Pins <c>startedAtUtc</c> to a fixed value.</summary>
    public AnyCulture WithStartedAtUtc(DateTimeOffset value) {
        return new AnyCulture(_id, _conditions, new FixedValue<DateTimeOffset>(value));
    }

    /// <summary>Draws <c>startedAtUtc</c> from <c>AnyValidStartedAtUtc()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCulture WithStartedAtUtc(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyCulture(_id, _conditions, configure(AnyValidStartedAtUtc()));
    }

    /// <summary>Produces one arbitrary <see cref="Culture" />.</summary>
    public Culture Generate() {
        return new Culture(_id.Generate(),
                           _conditions.Generate(),
                           _startedAtUtc.Generate());
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
