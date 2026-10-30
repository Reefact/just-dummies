// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Party --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Party" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyParty : IAny<Party> {

    private readonly IAny<string> _name;
    private readonly IAny<Iban>   _account;
    private readonly IAny<Bic>    _agent;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyParty()
        : this(name:    AnyValidName(),
               account: AnyValidAccount(),
               agent:   AnyValidAgent()) { }

    private static AnyString AnyValidName() {
        return Any.String().Printable().NotBlank().WithMaxLength(70);
    }

    private static AnyIban AnyValidAccount() {
        return new AnyIban().InCountry(DefaultCountry);
    }

    private static AnyBic AnyValidAgent() {
        return new AnyBic().InCountry(DefaultCountry);
    }

    private AnyParty(IAny<string> name,
                     IAny<Iban>   account,
                     IAny<Bic>    agent) {
        _name    = name;
        _account = account;
        _agent   = agent;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyParty WithName(string value) {
        return new AnyParty(new FixedValue<string>(value), _account, _agent);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyParty WithName(Func<AnyString, AnyString> configure) {
        return new AnyParty(configure(AnyValidName()), _account, _agent);
    }

    /// <summary>Pins <c>account</c> to a fixed value.</summary>
    public AnyParty WithAccount(Iban value) {
        return new AnyParty(_name, new FixedValue<Iban>(value), _agent);
    }

    /// <summary>Draws <c>account</c> from <c>AnyValidAccount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyParty WithAccount(Func<AnyIban, AnyIban> configure) {
        return new AnyParty(_name, configure(AnyValidAccount()), _agent);
    }

    /// <summary>Pins <c>agent</c> to a fixed value.</summary>
    public AnyParty WithAgent(Bic value) {
        return new AnyParty(_name, _account, new FixedValue<Bic>(value));
    }

    /// <summary>Draws <c>agent</c> from <c>AnyValidAgent()</c>, refined by <paramref name="configure" />.</summary>
    public AnyParty WithAgent(Func<AnyBic, AnyBic> configure) {
        return new AnyParty(_name, _account, configure(AnyValidAgent()));
    }

    /// <summary>Produces one arbitrary <see cref="Party" />.</summary>
    public Party Generate() {
        return new Party(_name.Generate(),
                         _account.Generate(),
                         _agent.Generate());
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
