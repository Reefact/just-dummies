// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ContainerId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ContainerId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContainerId : IAny<ContainerId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContainerId()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // Owner code, category U/J/Z and serial are drawn; the check digit is computed (ISO 6346).
        return Any.StringMatching("[A-Z]{3}[UJZ][0-9]{6}").As(body => body + ContainerId.ComputeCheckDigit(body));
    }

    private AnyContainerId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyContainerId WithValue(string value) {
        return new AnyContainerId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContainerId WithValue(Func<IAny<string>, IAny<string>> configure) {
        return new AnyContainerId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="ContainerId" />.</summary>
    public ContainerId Generate() {
        return new ContainerId(_value.Generate());
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
