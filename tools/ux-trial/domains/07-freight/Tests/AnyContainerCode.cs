// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ContainerCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ContainerCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContainerCode : IAny<ContainerCode> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContainerCode()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // ISO 6346: owner code, category, six-digit serial, then the check digit computed over those ten.
        return Any.Combine(Any.String().Alpha().InUpperCase().WithLength(3),
                           Any.Char().OneOf('U', 'J', 'Z'),
                           Any.Int32().Between(0, 999_999),
                           (owner, category, serial) => ContainerCode.Create(owner, category, serial).Value);
    }

    private AnyContainerCode(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyContainerCode WithValue(string value) {
        return new AnyContainerCode(new FixedValue<string>(value));
    }

    /// <summary>Produces one arbitrary <see cref="ContainerCode" />.</summary>
    public ContainerCode Generate() {
        return new ContainerCode(_value.Generate());
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
