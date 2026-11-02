// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Slot --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Slot" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySlot : IAny<Slot> {

    private IAny<IcaoAirportCode> _airport;
    private IAny<DateTime>        _start;
    private IAny<TimeSpan>        _duration;

    private AnyIcaoAirportCode _airportRecipe;
    private AnyDateTime        _startRecipe;
    private AnyTimeSpan        _durationRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySlot()
        : this(airport:  AnyValidAirport(),
               start:    AnyValidStart(),
               duration: AnyValidDuration()) { }

    private static AnyIcaoAirportCode AnyValidAirport() {
        return new AnyIcaoAirportCode();
    }

    private static AnyDateTime AnyValidStart() {
        return Any.DateTime().Between(new DateTime(2025, 1, 1), new DateTime(2035, 1, 1))
                  .WithGranularity(TimeSpan.FromMinutes(5));
    }

    private static AnyTimeSpan AnyValidDuration() {
        return Any.TimeSpan().Between(TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySlot(AnyIcaoAirportCode airport,
                    AnyDateTime        start,
                    AnyTimeSpan        duration) {
        _airport        = airport;
        _airportRecipe  = airport;
        _start          = start;
        _startRecipe    = start;
        _duration       = duration;
        _durationRecipe = duration;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySlot(AnySlot from) {
        _airport        = from._airport;
        _airportRecipe  = from._airportRecipe;
        _start          = from._start;
        _startRecipe    = from._startRecipe;
        _duration       = from._duration;
        _durationRecipe = from._durationRecipe;
    }

    /// <summary>Pins <c>airport</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySlot WithAirport(IcaoAirportCode value) {
        AnySlot copy = new AnySlot(this);

        copy._airport = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>airport</c> is drawn from — <c>AnyValidAirport()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIcaoAirportCode, AnyIcaoAirportCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySlot WithAirport(Func<AnyIcaoAirportCode, AnyIcaoAirportCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySlot copy = new AnySlot(this);

        copy._airportRecipe = configure(_airportRecipe);
        copy._airport       = copy._airportRecipe;

        return copy;
    }

    /// <summary>Pins <c>start</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySlot WithStart(DateTime value) {
        AnySlot copy = new AnySlot(this);

        copy._start = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>start</c> is drawn from — <c>AnyValidStart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySlot WithStart(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySlot copy = new AnySlot(this);

        copy._startRecipe = configure(_startRecipe);
        copy._start       = copy._startRecipe;

        return copy;
    }

    /// <summary>Pins <c>duration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySlot WithDuration(TimeSpan value) {
        AnySlot copy = new AnySlot(this);

        copy._duration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>duration</c> is drawn from — <c>AnyValidDuration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySlot WithDuration(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySlot copy = new AnySlot(this);

        copy._durationRecipe = configure(_durationRecipe);
        copy._duration       = copy._durationRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Slot" />.</summary>
    public Slot Generate() {
        return new Slot(_airport.Generate(),
                        _start.Generate(),
                        _duration.Generate());
    }

}
