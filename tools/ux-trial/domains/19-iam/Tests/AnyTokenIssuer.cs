// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TokenIssuer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TokenIssuer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTokenIssuer : IAny<TokenIssuer> {

    private readonly IAny<TokenPolicy> _policy;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTokenIssuer()
        : this(policy: AnyValidPolicy()) { }

    private static AnyTokenPolicy AnyValidPolicy() {
        return new AnyTokenPolicy();
    }

    private AnyTokenIssuer(IAny<TokenPolicy> policy) {
        _policy = policy;
    }

    /// <summary>Pins <c>policy</c> to a fixed value.</summary>
    public AnyTokenIssuer WithPolicy(TokenPolicy value) {
        return new AnyTokenIssuer(new FixedValue<TokenPolicy>(value));
    }

    /// <summary>Draws <c>policy</c> from <c>AnyValidPolicy()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTokenIssuer WithPolicy(Func<AnyTokenPolicy, AnyTokenPolicy> configure) {
        return new AnyTokenIssuer(configure(AnyValidPolicy()));
    }

    /// <summary>Produces one arbitrary <see cref="TokenIssuer" />.</summary>
    public TokenIssuer Generate() {
        return new TokenIssuer(_policy.Generate());
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
