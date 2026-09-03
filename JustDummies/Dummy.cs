namespace JustDummies;

/// <summary>
///     The entry point of the library: supplies arbitrary, valid values for the parts of a test that are <b>not</b>
///     under assertion — the <i>dummies</i> a test needs so its <c>Arrange</c> stops advertising values it never
///     checks. The constraints chained on a generator express what the surrounding code requires of the value (a
///     value object's invariant, a contract precondition), never what the test asserts: an explicit <see cref="Dummy" />
///     call reads as "this is arbitrary" where a hand-picked literal reads as "this matters".
/// </summary>
/// <remarks>
///     <para>
///         Values are <b>built to satisfy</b> the declared constraints — the library never generates candidates and
///         filters them afterwards. Constraints that contradict each other fail at declaration time with a
///         <see cref="ConflictingDummyConstraintException" /> naming both sides.
///     </para>
///     <para>
///         Every value is drawn from a pseudo-random source. By default that source is unseeded, so each run produces
///         fresh values — which surfaces a test that secretly depends on one. Wrap a value-sensitive test in
///         <see cref="Reproducibly(Action, Action{string})" /> to make a failing run replayable: the source flows with
///         the current execution context, so it never leaks across tests running in parallel. For an explicit,
///         isolated deterministic context — for example outside a test body — use <see cref="WithSeed" />.
///     </para>
///     <example>
///         <code>
///         // The reference format is the invariant; the exact value is irrelevant — so it is Dummy.
///         string reference = Dummy.String().StartingWith("ORD-").WithLength(12).Generate();
///
///         // Turn a constrained primitive into a value object, without reflection:
///         OrderReference order = Dummy.String().StartingWith("ORD-").WithLength(12)
///                                   .As(OrderReference.Create)
///                                   .Generate();
///
///         // Make a value-sensitive test replayable: the seed is reported on failure...
///         Dummy.Reproducibly(() => { /* arrange with Dummy, act, assert */ });
///         // ...and replayed by passing it back:
///         Dummy.Reproducibly(1234, () => { /* ... */ });
///         </code>
///     </example>
/// </remarks>
// This file carries the façade's documentation and no member: every entry point lives in a sibling partial named
// after its family (Dummy.Primitive.cs, Dummy.Pattern.cs, Dummy.Uri.cs, Dummy.Choice.cs, Dummy.Collection.cs, Dummy.Combine.cs,
// Dummy.Reproducibility.cs). A family gets its own file as soon as its entry point returns a *narrowing builder* rather
// than a constrained scalar, however few members that leaves here — the file's weight is the surface it opens, not
// its line count. Adding a member to any of them means mirroring it on DummyContext; SurfaceParityTests enforces that.
public static partial class Dummy { }
