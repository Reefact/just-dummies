// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Account --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Account" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAccount : IAny<Account> {

    private readonly IAny<string>         _accountNumber;
    private readonly IAny<Holder>         _holder;
    private readonly IAny<AccountKind>    _kind;
    private readonly IAny<DateTimeOffset> _openedOn;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAccount()
        : this(accountNumber: AnyValidAccountNumber(),
               holder:        AnyValidHolder(),
               kind:          AnyValidKind(),
               openedOn:      AnyValidOpenedOn()) { }

    // The account id ties two parameters: "CC-NNNNNN", where CC is the holder's country code.
    // Only the six-digit number is drawn here; Generate() prefixes it with the drawn holder's country,
    // so the rule holds by construction rather than by retry.
    private static AnyString AnyValidAccountNumber() {
        return Any.String().Numeric().WithLength(6);
    }

    private static AnyHolder AnyValidHolder() {
        return new AnyHolder();
    }

    private static AnyEnum<AccountKind> AnyValidKind() {
        return Any.Enum<AccountKind>();
    }

    private static AnyDateTimeOffset AnyValidOpenedOn() {
        return Any.DateTimeOffset();
    }

    private AnyAccount(IAny<string>         accountNumber,
                       IAny<Holder>         holder,
                       IAny<AccountKind>    kind,
                       IAny<DateTimeOffset> openedOn) {
        _accountNumber = accountNumber;
        _holder        = holder;
        _kind          = kind;
        _openedOn      = openedOn;
    }

    /// <summary>Draws the six digits after the country prefix from <c>AnyValidAccountNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccount WithAccountNumber(Func<AnyString, AnyString> configure) {
        return new AnyAccount(configure(AnyValidAccountNumber()), _holder, _kind, _openedOn);
    }

    /// <summary>Pins <c>holder</c> to a fixed value.</summary>
    public AnyAccount WithHolder(Holder value) {
        return new AnyAccount(_accountNumber, new FixedValue<Holder>(value), _kind, _openedOn);
    }

    /// <summary>Draws <c>holder</c> from <c>AnyValidHolder()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccount WithHolder(Func<AnyHolder, AnyHolder> configure) {
        return new AnyAccount(_accountNumber, configure(AnyValidHolder()), _kind, _openedOn);
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyAccount WithKind(AccountKind value) {
        return new AnyAccount(_accountNumber, _holder, new FixedValue<AccountKind>(value), _openedOn);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccount WithKind(Func<AnyEnum<AccountKind>, AnyEnum<AccountKind>> configure) {
        return new AnyAccount(_accountNumber, _holder, configure(AnyValidKind()), _openedOn);
    }

    /// <summary>Pins <c>openedOn</c> to a fixed value.</summary>
    public AnyAccount WithOpenedOn(DateTimeOffset value) {
        return new AnyAccount(_accountNumber, _holder, _kind, new FixedValue<DateTimeOffset>(value));
    }

    /// <summary>Draws <c>openedOn</c> from <c>AnyValidOpenedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccount WithOpenedOn(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyAccount(_accountNumber, _holder, _kind, configure(AnyValidOpenedOn()));
    }

    /// <summary>Produces one arbitrary <see cref="Account" />.</summary>
    public Account Generate() {
        Holder holder = _holder.Generate();

        return new Account($"{holder.CountryCode}-{_accountNumber.Generate()}",
                           holder,
                           _kind.Generate(),
                           _openedOn.Generate());
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
