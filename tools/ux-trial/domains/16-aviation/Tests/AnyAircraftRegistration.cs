// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AircraftRegistration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AircraftRegistration" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAircraftRegistration : IAny<AircraftRegistration> {

    private readonly IAny<string> _mark;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAircraftRegistration()
        : this(mark: AnyValidMark()) { }

    private static AnyPattern AnyValidMark() {
        // The scaffolder reported this parameter as inferred and silent, but the constructor's switch
        // expression accepts only two formats, at most seven characters long. Fixed by hand.
        return Any.StringMatching("^([A-MO-Z][A-Z]?-[A-Z]{3,4}|N[1-9][0-9]{0,3}[A-Z]{0,2})$");
    }

    private AnyAircraftRegistration(IAny<string> mark) {
        _mark = mark;
    }

    /// <summary>Pins <c>mark</c> to a fixed value.</summary>
    public AnyAircraftRegistration WithMark(string value) {
        return new AnyAircraftRegistration(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>mark</c> from <c>AnyValidMark()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAircraftRegistration WithMark(Func<AnyPattern, AnyPattern> configure) {
        return new AnyAircraftRegistration(configure(AnyValidMark()));
    }

    /// <summary>Produces one arbitrary <see cref="AircraftRegistration" />.</summary>
    public AircraftRegistration Generate() {
        return new AircraftRegistration(_mark.Generate());
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
