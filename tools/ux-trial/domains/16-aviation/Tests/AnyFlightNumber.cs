// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FlightNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="FlightNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFlightNumber : IAny<FlightNumber> {

    private readonly IAny<string> _airlineDesignator;
    private readonly IAny<int>    _number;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFlightNumber()
        : this(airlineDesignator: AnyValidAirlineDesignator(),
               number:            AnyValidNumber()) { }

    private static AnyString AnyValidAirlineDesignator() {
        // Validate(...) demands ^[A-Z0-9]{2}$.
        return Any.String().AlphaNumeric().InUpperCase().WithLength(2);
    }

    private static AnyInt32 AnyValidNumber() {
        // Validate(...) demands 1 to 9999.
        return Any.Int32().Between(1, 9999);
    }

    private AnyFlightNumber(IAny<string> airlineDesignator,
                            IAny<int>    number) {
        _airlineDesignator = airlineDesignator;
        _number            = number;
    }

    /// <summary>Pins <c>airlineDesignator</c> to a fixed value.</summary>
    public AnyFlightNumber WithAirlineDesignator(string value) {
        return new AnyFlightNumber(new FixedValue<string>(value), _number);
    }

    /// <summary>Draws <c>airlineDesignator</c> from <c>AnyValidAirlineDesignator()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFlightNumber WithAirlineDesignator(Func<AnyString, AnyString> configure) {
        return new AnyFlightNumber(configure(AnyValidAirlineDesignator()), _number);
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyFlightNumber WithNumber(int value) {
        return new AnyFlightNumber(_airlineDesignator, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFlightNumber WithNumber(Func<AnyInt32, AnyInt32> configure) {
        return new AnyFlightNumber(_airlineDesignator, configure(AnyValidNumber()));
    }

    /// <summary>Produces one arbitrary <see cref="FlightNumber" />.</summary>
    public FlightNumber Generate() {
        return new FlightNumber(_airlineDesignator.Generate(),
                                _number.Generate());
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
