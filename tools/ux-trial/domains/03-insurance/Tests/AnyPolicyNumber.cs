// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PolicyNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PolicyNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPolicyNumber : IAny<PolicyNumber> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPolicyNumber()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // PolicyNumber checks ^POL-\d{4}-[A-Z]{2}\d{6}$ through a Regex the scaffolder cannot read.
        return Any.StringMatching(@"POL-\d{4}-[A-Z]{2}\d{6}");
    }

    private AnyPolicyNumber(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyPolicyNumber WithValue(string value) {
        return new AnyPolicyNumber(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPolicyNumber WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyPolicyNumber(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="PolicyNumber" />.</summary>
    public PolicyNumber Generate() {
        return new PolicyNumber(_value.Generate());
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
