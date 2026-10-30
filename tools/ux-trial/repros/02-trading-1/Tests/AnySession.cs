// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Session --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Repro.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Session" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySession : IAny<Session> {

    private readonly IAny<DateTime> _opensAt;
    private readonly IAny<DateTime> _closesAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySession()
        : this(opensAt:  AnyValidOpensAt(),
               closesAt: AnyValidClosesAt()) { }

    private static AnyDateTime AnyValidOpensAt() {
        // TODO(any): 'DateTime opensAt' may be guarded by something the scaffolder could not read (§9).
        //   This is the scaffolder's best generator for the type; verify it honours the real invariant,
        //   or replace it, then delete the line below.

        return Any.DateTime();
    }

    private static AnyDateTime AnyValidClosesAt() {
        // TODO(any): 'DateTime closesAt' may be guarded by something the scaffolder could not read (§9).
        //   This is the scaffolder's best generator for the type; verify it honours the real invariant,
        //   or replace it, then delete the line below.

        return Any.DateTime();
    }

    private AnySession(IAny<DateTime> opensAt,
                       IAny<DateTime> closesAt) {
        _opensAt  = opensAt;
        _closesAt = closesAt;
    }

    /// <summary>Pins <c>opensAt</c> to a fixed value.</summary>
    public AnySession WithOpensAt(DateTime value) {
        return new AnySession(new FixedValue<DateTime>(value), _closesAt);
    }

    /// <summary>Draws <c>opensAt</c> from <c>AnyValidOpensAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnySession WithOpensAt(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnySession(configure(AnyValidOpensAt()), _closesAt);
    }

    /// <summary>Pins <c>closesAt</c> to a fixed value.</summary>
    public AnySession WithClosesAt(DateTime value) {
        return new AnySession(_opensAt, new FixedValue<DateTime>(value));
    }

    /// <summary>Draws <c>closesAt</c> from <c>AnyValidClosesAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnySession WithClosesAt(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnySession(_opensAt, configure(AnyValidClosesAt()));
    }

    /// <summary>Produces one arbitrary <see cref="Session" />.</summary>
    public Session Generate() {
        return new Session(_opensAt.Generate(),
                           _closesAt.Generate());
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
