// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ClientId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ClientId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyClientId : IAny<ClientId> {

    private readonly IAny<Guid> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyClientId()
        : this(value: AnyValidValue()) { }

    private static AnyGuid AnyValidValue() {
        return Any.Guid().NonEmpty();
    }

    private AnyClientId(IAny<Guid> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyClientId WithValue(Guid value) {
        return new AnyClientId(new FixedValue<Guid>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyClientId WithValue(Func<AnyGuid, AnyGuid> configure) {
        return new AnyClientId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="ClientId" />.</summary>
    public ClientId Generate() {
        return new ClientId(_value.Generate());
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
