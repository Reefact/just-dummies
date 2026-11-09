// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Week --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GuardMiss.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Week" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWeek : IAny<Week> {

    private readonly IAny<string> _week;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWeek()
        : this(week: AnyValidWeek()) { }

    private static AnyString AnyValidWeek() {
        return Any.String().NonEmpty();
    }

    private AnyWeek(IAny<string> week) {
        _week = week;
    }

    /// <summary>Pins <c>week</c> to a fixed value.</summary>
    public AnyWeek WithWeek(string value) {
        return new AnyWeek(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>week</c> from <c>AnyValidWeek()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWeek WithWeek(Func<AnyString, AnyString> configure) {
        return new AnyWeek(configure(AnyValidWeek()));
    }

    /// <summary>Produces one arbitrary <see cref="Week" />.</summary>
    public Week Generate() {
        return new Week(_week.Generate());
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
