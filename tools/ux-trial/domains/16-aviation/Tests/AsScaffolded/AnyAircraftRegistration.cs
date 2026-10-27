// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AircraftRegistration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using Aviation.Domain;
using JustDummies;

namespace Aviation.Tests.AsScaffolded;

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

    private static AnyString AnyValidMark() {
        return Any.String().NonEmpty();
    }

    private AnyAircraftRegistration(IAny<string> mark) {
        _mark = mark;
    }

    /// <summary>Pins <c>mark</c> to a fixed value.</summary>
    public AnyAircraftRegistration WithMark(string value) {
        return new AnyAircraftRegistration(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>mark</c> from <c>AnyValidMark()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAircraftRegistration WithMark(Func<AnyString, AnyString> configure) {
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
