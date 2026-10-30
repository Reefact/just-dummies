// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Transfer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Transfer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTransfer : IAny<Transfer> {

    private readonly IAny<CadastralReference> _parcel;
    private readonly IAny<decimal>            _price;
    private readonly IAny<DateOnly>           _signedOn;
    private readonly IAny<DateOnly>           _registeredOn;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTransfer()
        : this(parcel:       AnyValidParcel(),
               price:        AnyValidPrice(),
               signedOn:     AnyValidSignedOn(),
               registeredOn: AnyValidRegisteredOn()) { }

    private static AnyCadastralReference AnyValidParcel() {
        return new AnyCadastralReference();
    }

    private static AnyDecimal AnyValidPrice() {
        return Any.Decimal().Positive().LessThanOrEqualTo(1000000000m);
    }

    private static AnyDateOnly AnyValidSignedOn() {
        return Any.DateOnly();
    }

    private static AnyDateOnly AnyValidRegisteredOn() {
        // Registration happens on or after signing: drawn as a delay from the signing date in Generate().
        return Any.DateOnly();
    }

    private AnyTransfer(IAny<CadastralReference> parcel,
                        IAny<decimal>            price,
                        IAny<DateOnly>           signedOn,
                        IAny<DateOnly>           registeredOn) {
        _parcel       = parcel;
        _price        = price;
        _signedOn     = signedOn;
        _registeredOn = registeredOn;
    }

    /// <summary>Pins <c>parcel</c> to a fixed value.</summary>
    public AnyTransfer WithParcel(CadastralReference value) {
        return new AnyTransfer(new FixedValue<CadastralReference>(value), _price, _signedOn, _registeredOn);
    }

    /// <summary>Draws <c>parcel</c> from <c>AnyValidParcel()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransfer WithParcel(Func<AnyCadastralReference, AnyCadastralReference> configure) {
        return new AnyTransfer(configure(AnyValidParcel()), _price, _signedOn, _registeredOn);
    }

    /// <summary>Pins <c>price</c> to a fixed value.</summary>
    public AnyTransfer WithPrice(decimal value) {
        return new AnyTransfer(_parcel, new FixedValue<decimal>(value), _signedOn, _registeredOn);
    }

    /// <summary>Draws <c>price</c> from <c>AnyValidPrice()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransfer WithPrice(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyTransfer(_parcel, configure(AnyValidPrice()), _signedOn, _registeredOn);
    }

    /// <summary>Pins <c>signedOn</c> to a fixed value.</summary>
    public AnyTransfer WithSignedOn(DateOnly value) {
        return new AnyTransfer(_parcel, _price, new FixedValue<DateOnly>(value), _registeredOn);
    }

    /// <summary>Draws <c>signedOn</c> from <c>AnyValidSignedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransfer WithSignedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyTransfer(_parcel, _price, configure(AnyValidSignedOn()), _registeredOn);
    }

    /// <summary>Pins <c>registeredOn</c> to a fixed value.</summary>
    public AnyTransfer WithRegisteredOn(DateOnly value) {
        return new AnyTransfer(_parcel, _price, _signedOn, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>registeredOn</c> from <c>AnyValidRegisteredOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransfer WithRegisteredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyTransfer(_parcel, _price, _signedOn, configure(AnyValidRegisteredOn()));
    }

    /// <summary>Produces one arbitrary <see cref="Transfer" />.</summary>
    public Transfer Generate() {
        DateOnly signedOn     = _signedOn.Generate();
        DateOnly registeredOn = _registeredOn.Generate();
        // Hand-edited: registration never precedes signing, a cross-field rule the independent
        // per-parameter generators cannot hold; ordering the two draws keeps both arbitrary.
        if (registeredOn < signedOn) {
            (signedOn, registeredOn) = (registeredOn, signedOn);
        }
        return new Transfer(_parcel.Generate(),
                            _price.Generate(),
                            signedOn,
                            registeredOn);
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
