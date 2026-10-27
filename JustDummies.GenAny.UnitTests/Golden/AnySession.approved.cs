// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Session --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

/// <summary>
///     A generator of arbitrary <see cref="Session" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySession : IAny<Session> {

    /// <summary>Creates the generator.</summary>
    public AnySession() { }

    /// <summary>Produces one arbitrary <see cref="Session" />.</summary>
    public Session Generate() {
        return new Session();
    }

}
