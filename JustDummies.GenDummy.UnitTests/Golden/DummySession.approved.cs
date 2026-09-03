// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Session --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

/// <summary>
///     A generator of arbitrary <see cref="Session" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins it; to draw from an isolated
///     <c>Dummy.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
/// </summary>
public sealed partial class DummySession : IDummy<Session> {

    /// <summary>Creates the generator.</summary>
    public DummySession() { }

    /// <summary>Produces one arbitrary <see cref="Session" />.</summary>
    public Session Generate() {
        return new Session();
    }

}
