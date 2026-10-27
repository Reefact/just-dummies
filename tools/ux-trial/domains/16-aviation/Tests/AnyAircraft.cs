// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Aircraft --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Aircraft" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAircraft : IAny<Aircraft> {

    private readonly IAny<AircraftRegistration> _registration;
    private readonly IAny<string>               _typeDesignator;
    private readonly IAny<int>                  _seats;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAircraft()
        : this(registration:   AnyValidRegistration(),
               typeDesignator: AnyValidTypeDesignator(),
               seats:          AnyValidSeats()) { }

    private static AnyAircraftRegistration AnyValidRegistration() {
        return new AnyAircraftRegistration();
    }

    private static AnyString AnyValidTypeDesignator() {
        // The constructor only accepts a type from the fleet reference list.
        return Any.String().OneOf(Aircraft.KnownTypes);
    }

    private static AnyInt32 AnyValidSeats() {
        // The ceiling depends on the type (cross-field); 78, the smallest type's maximum, is valid for every type.
        return Any.Int32().Between(1, 78);
    }

    private AnyAircraft(IAny<AircraftRegistration> registration,
                        IAny<string>               typeDesignator,
                        IAny<int>                  seats) {
        _registration   = registration;
        _typeDesignator = typeDesignator;
        _seats          = seats;
    }

    /// <summary>Pins <c>registration</c> to a fixed value.</summary>
    public AnyAircraft WithRegistration(AircraftRegistration value) {
        return new AnyAircraft(new FixedValue<AircraftRegistration>(value), _typeDesignator, _seats);
    }

    /// <summary>Draws <c>registration</c> from <c>AnyValidRegistration()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAircraft WithRegistration(Func<AnyAircraftRegistration, AnyAircraftRegistration> configure) {
        return new AnyAircraft(configure(AnyValidRegistration()), _typeDesignator, _seats);
    }

    /// <summary>Pins <c>typeDesignator</c> to a fixed value.</summary>
    public AnyAircraft WithTypeDesignator(string value) {
        return new AnyAircraft(_registration, new FixedValue<string>(value), _seats);
    }

    /// <summary>Draws <c>typeDesignator</c> from <c>AnyValidTypeDesignator()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAircraft WithTypeDesignator(Func<AnyString, AnyString> configure) {
        return new AnyAircraft(_registration, configure(AnyValidTypeDesignator()), _seats);
    }

    /// <summary>Pins <c>seats</c> to a fixed value.</summary>
    public AnyAircraft WithSeats(int value) {
        return new AnyAircraft(_registration, _typeDesignator, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>seats</c> from <c>AnyValidSeats()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAircraft WithSeats(Func<AnyInt32, AnyInt32> configure) {
        return new AnyAircraft(_registration, _typeDesignator, configure(AnyValidSeats()));
    }

    /// <summary>Produces one arbitrary <see cref="Aircraft" />.</summary>
    public Aircraft Generate() {
        return new Aircraft(_registration.Generate(),
                            _typeDesignator.Generate(),
                            _seats.Generate());
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
