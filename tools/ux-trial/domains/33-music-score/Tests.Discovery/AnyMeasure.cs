// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Measure --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Measure" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasure : IAny<Measure> {

    private IAny<int>                  _number;
    private IAny<TimeSignature>        _timeSignature;
    private IAny<Dynamic>              _dynamic;
    private IAny<IReadOnlyList<Voice>>? _voices; // null: voices are drawn to fill the time signature
    private IAny<bool>                 _repeatStart;
    private IAny<int>                  _repeatEndTimes;

    private AnyInt32         _numberRecipe;
    private AnyTimeSignature _timeSignatureRecipe;
    private AnyEnum<Dynamic> _dynamicRecipe;
    private AnyBoolean       _repeatStartRecipe;
    private AnyInt32         _repeatEndTimesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasure()
        : this(number:         AnyValidNumber(),
               timeSignature:  AnyValidTimeSignature(),
               dynamic:        AnyValidDynamic(),
               repeatStart:    AnyValidRepeatStart(),
               repeatEndTimes: AnyValidRepeatEndTimes()) { }

    private static AnyInt32 AnyValidNumber() {
        return Any.Int32().Positive();
    }

    private static AnyTimeSignature AnyValidTimeSignature() {
        return new AnyTimeSignature();
    }

    private static AnyEnum<Dynamic> AnyValidDynamic() {
        return Any.Enum<Dynamic>();
    }

    private static AnyBoolean AnyValidRepeatStart() {
        return Any.Boolean();
    }

    private static AnyInt32 AnyValidRepeatEndTimes() {
        // 0 means "no end repeat"; otherwise the section is played 2 to 8 times.
        return Any.Int32().Between(0, Measure.MaxRepeatTimes).Except(1);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMeasure(AnyInt32         number,
                       AnyTimeSignature timeSignature,
                       AnyEnum<Dynamic> dynamic,
                       AnyBoolean       repeatStart,
                       AnyInt32         repeatEndTimes) {
        _number               = number;
        _numberRecipe         = number;
        _timeSignature        = timeSignature;
        _timeSignatureRecipe  = timeSignature;
        _dynamic              = dynamic;
        _dynamicRecipe        = dynamic;
        _voices               = null;
        _repeatStart          = repeatStart;
        _repeatStartRecipe    = repeatStart;
        _repeatEndTimes       = repeatEndTimes;
        _repeatEndTimesRecipe = repeatEndTimes;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMeasure(AnyMeasure from) {
        _number               = from._number;
        _numberRecipe         = from._numberRecipe;
        _timeSignature        = from._timeSignature;
        _timeSignatureRecipe  = from._timeSignatureRecipe;
        _dynamic              = from._dynamic;
        _dynamicRecipe        = from._dynamicRecipe;
        _voices               = from._voices;
        _repeatStart          = from._repeatStart;
        _repeatStartRecipe    = from._repeatStartRecipe;
        _repeatEndTimes       = from._repeatEndTimes;
        _repeatEndTimesRecipe = from._repeatEndTimesRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasure WithNumber(int value) {
        AnyMeasure copy = new AnyMeasure(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasure WithNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasure copy = new AnyMeasure(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>timeSignature</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasure WithTimeSignature(TimeSignature value) {
        AnyMeasure copy = new AnyMeasure(this);

        copy._timeSignature = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>timeSignature</c> is drawn from — <c>AnyValidTimeSignature()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSignature, AnyTimeSignature&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasure WithTimeSignature(Func<AnyTimeSignature, AnyTimeSignature> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasure copy = new AnyMeasure(this);

        copy._timeSignatureRecipe = configure(_timeSignatureRecipe);
        copy._timeSignature       = copy._timeSignatureRecipe;

        return copy;
    }

    /// <summary>Pins <c>dynamic</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasure WithDynamic(Dynamic value) {
        AnyMeasure copy = new AnyMeasure(this);

        copy._dynamic = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dynamic</c> is drawn from — <c>AnyValidDynamic()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;Dynamic&gt;, AnyEnum&lt;Dynamic&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasure WithDynamic(Func<AnyEnum<Dynamic>, AnyEnum<Dynamic>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasure copy = new AnyMeasure(this);

        copy._dynamicRecipe = configure(_dynamicRecipe);
        copy._dynamic       = copy._dynamicRecipe;

        return copy;
    }

    /// <summary>Pins <c>voices</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasure WithVoices(IReadOnlyList<Voice> value) {
        AnyMeasure copy = new AnyMeasure(this);

        copy._voices = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>repeatStart</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasure WithRepeatStart(bool value) {
        AnyMeasure copy = new AnyMeasure(this);

        copy._repeatStart = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>repeatStart</c> is drawn from — <c>AnyValidRepeatStart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasure WithRepeatStart(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasure copy = new AnyMeasure(this);

        copy._repeatStartRecipe = configure(_repeatStartRecipe);
        copy._repeatStart       = copy._repeatStartRecipe;

        return copy;
    }

    /// <summary>Pins <c>repeatEndTimes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasure WithRepeatEndTimes(int value) {
        AnyMeasure copy = new AnyMeasure(this);

        copy._repeatEndTimes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>repeatEndTimes</c> is drawn from — <c>AnyValidRepeatEndTimes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasure WithRepeatEndTimes(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasure copy = new AnyMeasure(this);

        copy._repeatEndTimesRecipe = configure(_repeatEndTimesRecipe);
        copy._repeatEndTimes       = copy._repeatEndTimesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Measure" />.</summary>
    public Measure Generate() {
        int           number        = _number.Generate();
        TimeSignature timeSignature = _timeSignature.Generate();
        Dynamic       dynamic       = _dynamic.Generate();

        // Every voice of a measure must fill the meter exactly, so voices are drawn once the meter is known.
        IReadOnlyList<Voice> voices = _voices is null ? DrawVoicesFilling(timeSignature) : _voices.Generate();

        return new Measure(number,
                           timeSignature,
                           dynamic,
                           voices,
                           _repeatStart.Generate(),
                           _repeatEndTimes.Generate());
    }

}
