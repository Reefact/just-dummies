// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VoiceEntry --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VoiceEntry" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVoiceEntry : IAny<VoiceEntry> {

    private IAny<Duration> _duration;
    private IAny<Pitch?>   _pitch;
    private IAny<bool>     _tiedToNext;

    private AnyDuration _durationRecipe;
    private AnyPitch    _pitchRecipe;
    private AnyBoolean  _tiedToNextRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVoiceEntry()
        : this(duration:   AnyValidDuration(),
               pitch:      AnyValidPitch(),
               tiedToNext: AnyValidTiedToNext()) { }

    private static AnyDuration AnyValidDuration() {
        return new AnyDuration().WithNumerator(n => n.Positive());
    }

    private static AnyPitch AnyValidPitch() {
        return new AnyPitch();
    }

    private static AnyBoolean AnyValidTiedToNext() {
        return Any.Boolean();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVoiceEntry(AnyDuration duration,
                          AnyPitch    pitch,
                          AnyBoolean  tiedToNext) {
        _duration         = duration;
        _durationRecipe   = duration;
        _pitch            = pitch.OrNull();
        _pitchRecipe      = pitch;
        _tiedToNext       = tiedToNext;
        _tiedToNextRecipe = tiedToNext;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVoiceEntry(AnyVoiceEntry from) {
        _duration         = from._duration;
        _durationRecipe   = from._durationRecipe;
        _pitch            = from._pitch;
        _pitchRecipe      = from._pitchRecipe;
        _tiedToNext       = from._tiedToNext;
        _tiedToNextRecipe = from._tiedToNextRecipe;
    }

    /// <summary>Pins <c>duration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVoiceEntry WithDuration(Duration value) {
        AnyVoiceEntry copy = new AnyVoiceEntry(this);

        copy._duration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>duration</c> is drawn from — <c>AnyValidDuration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDuration, AnyDuration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoiceEntry WithDuration(Func<AnyDuration, AnyDuration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoiceEntry copy = new AnyVoiceEntry(this);

        copy._durationRecipe = configure(_durationRecipe);
        copy._duration       = copy._durationRecipe;

        return copy;
    }

    /// <summary>Pins <c>pitch</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVoiceEntry WithPitch(Pitch? value) {
        AnyVoiceEntry copy = new AnyVoiceEntry(this);

        copy._pitch = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pitch</c> is drawn from — <c>AnyValidPitch()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPitch, AnyPitch&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoiceEntry WithPitch(Func<AnyPitch, AnyPitch> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoiceEntry copy = new AnyVoiceEntry(this);

        copy._pitchRecipe = configure(_pitchRecipe);
        copy._pitch       = copy._pitchRecipe;

        return copy;
    }

    /// <summary>Pins <c>pitch</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyVoiceEntry WithoutPitch() {
        AnyVoiceEntry copy = new AnyVoiceEntry(this);

        copy._pitch = Any.Fixed<Pitch?>(null);

        return copy;
    }

    /// <summary>Pins <c>tiedToNext</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVoiceEntry WithTiedToNext(bool value) {
        AnyVoiceEntry copy = new AnyVoiceEntry(this);

        copy._tiedToNext = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tiedToNext</c> is drawn from — <c>AnyValidTiedToNext()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoiceEntry WithTiedToNext(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoiceEntry copy = new AnyVoiceEntry(this);

        copy._tiedToNextRecipe = configure(_tiedToNextRecipe);
        copy._tiedToNext       = copy._tiedToNextRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VoiceEntry" />.</summary>
    public VoiceEntry Generate() {
        Duration duration   = _duration.Generate();
        Pitch?   pitch      = _pitch.Generate();
        bool     tiedToNext = _tiedToNext.Generate();

        // A rest cannot be tied: the tie is only kept for a note.
        return new VoiceEntry(duration, pitch, pitch is not null && tiedToNext);
    }

}
