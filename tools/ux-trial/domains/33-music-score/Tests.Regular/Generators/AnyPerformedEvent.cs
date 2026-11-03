// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PerformedEvent --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PerformedEvent" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPerformedEvent : IAny<PerformedEvent> {

    private IAny<string>   _staffName;
    private IAny<int>      _voiceNumber;
    private IAny<Pitch>    _pitch;
    private IAny<int>      _velocity;
    private IAny<Duration> _onset;
    private IAny<Duration> _length;
    private IAny<double>   _startSeconds;
    private IAny<double>   _durationSeconds;

    private AnyString   _staffNameRecipe;
    private AnyInt32    _voiceNumberRecipe;
    private AnyPitch    _pitchRecipe;
    private AnyInt32    _velocityRecipe;
    private AnyDuration _onsetRecipe;
    private AnyDuration _lengthRecipe;
    private AnyDouble   _startSecondsRecipe;
    private AnyDouble   _durationSecondsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPerformedEvent()
        : this(staffName:       AnyValidStaffName(),
               voiceNumber:     AnyValidVoiceNumber(),
               pitch:           AnyValidPitch(),
               velocity:        AnyValidVelocity(),
               onset:           AnyValidOnset(),
               length:          AnyValidLength(),
               startSeconds:    AnyValidStartSeconds(),
               durationSeconds: AnyValidDurationSeconds()) { }

    private static AnyString AnyValidStaffName() {
        return Any.String().NotBlank().WithMaxLength(40);
    }

    private static AnyInt32 AnyValidVoiceNumber() {
        return Any.Int32().Positive().LessThanOrEqualTo(Voice.MaxVoices);
    }

    private static AnyPitch AnyValidPitch() {
        return new AnyPitch();
    }

    private static AnyInt32 AnyValidVelocity() {
        return Any.Int32().Positive().LessThanOrEqualTo(127);
    }

    private static AnyDuration AnyValidOnset() {
        return new AnyDuration();
    }

    private static AnyDuration AnyValidLength() {
        return new AnyDuration().WithNumerator(n => n.Positive());
    }

    private static AnyDouble AnyValidStartSeconds() {
        return Any.Double().Between(0d, 10_000d);
    }

    private static AnyDouble AnyValidDurationSeconds() {
        return Any.Double().Between(0.001d, 10_000d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPerformedEvent(AnyString   staffName,
                              AnyInt32    voiceNumber,
                              AnyPitch    pitch,
                              AnyInt32    velocity,
                              AnyDuration onset,
                              AnyDuration length,
                              AnyDouble   startSeconds,
                              AnyDouble   durationSeconds) {
        _staffName             = staffName;
        _staffNameRecipe       = staffName;
        _voiceNumber           = voiceNumber;
        _voiceNumberRecipe     = voiceNumber;
        _pitch                 = pitch;
        _pitchRecipe           = pitch;
        _velocity              = velocity;
        _velocityRecipe        = velocity;
        _onset                 = onset;
        _onsetRecipe           = onset;
        _length                = length;
        _lengthRecipe          = length;
        _startSeconds          = startSeconds;
        _startSecondsRecipe    = startSeconds;
        _durationSeconds       = durationSeconds;
        _durationSecondsRecipe = durationSeconds;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPerformedEvent(AnyPerformedEvent from) {
        _staffName             = from._staffName;
        _staffNameRecipe       = from._staffNameRecipe;
        _voiceNumber           = from._voiceNumber;
        _voiceNumberRecipe     = from._voiceNumberRecipe;
        _pitch                 = from._pitch;
        _pitchRecipe           = from._pitchRecipe;
        _velocity              = from._velocity;
        _velocityRecipe        = from._velocityRecipe;
        _onset                 = from._onset;
        _onsetRecipe           = from._onsetRecipe;
        _length                = from._length;
        _lengthRecipe          = from._lengthRecipe;
        _startSeconds          = from._startSeconds;
        _startSecondsRecipe    = from._startSecondsRecipe;
        _durationSeconds       = from._durationSeconds;
        _durationSecondsRecipe = from._durationSecondsRecipe;
    }

    /// <summary>Pins <c>staffName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPerformedEvent WithStaffName(string value) {
        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._staffName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>staffName</c> is drawn from — <c>AnyValidStaffName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPerformedEvent WithStaffName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._staffNameRecipe = configure(_staffNameRecipe);
        copy._staffName       = copy._staffNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>voiceNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPerformedEvent WithVoiceNumber(int value) {
        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._voiceNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>voiceNumber</c> is drawn from — <c>AnyValidVoiceNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPerformedEvent WithVoiceNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._voiceNumberRecipe = configure(_voiceNumberRecipe);
        copy._voiceNumber       = copy._voiceNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>pitch</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPerformedEvent WithPitch(Pitch value) {
        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._pitch = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pitch</c> is drawn from — <c>AnyValidPitch()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPitch, AnyPitch&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPerformedEvent WithPitch(Func<AnyPitch, AnyPitch> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._pitchRecipe = configure(_pitchRecipe);
        copy._pitch       = copy._pitchRecipe;

        return copy;
    }

    /// <summary>Pins <c>velocity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPerformedEvent WithVelocity(int value) {
        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._velocity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>velocity</c> is drawn from — <c>AnyValidVelocity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPerformedEvent WithVelocity(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._velocityRecipe = configure(_velocityRecipe);
        copy._velocity       = copy._velocityRecipe;

        return copy;
    }

    /// <summary>Pins <c>onset</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPerformedEvent WithOnset(Duration value) {
        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._onset = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>onset</c> is drawn from — <c>AnyValidOnset()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDuration, AnyDuration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPerformedEvent WithOnset(Func<AnyDuration, AnyDuration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._onsetRecipe = configure(_onsetRecipe);
        copy._onset       = copy._onsetRecipe;

        return copy;
    }

    /// <summary>Pins <c>length</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPerformedEvent WithLength(Duration value) {
        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._length = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>length</c> is drawn from — <c>AnyValidLength()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDuration, AnyDuration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPerformedEvent WithLength(Func<AnyDuration, AnyDuration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._lengthRecipe = configure(_lengthRecipe);
        copy._length       = copy._lengthRecipe;

        return copy;
    }

    /// <summary>Pins <c>startSeconds</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPerformedEvent WithStartSeconds(double value) {
        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._startSeconds = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>startSeconds</c> is drawn from — <c>AnyValidStartSeconds()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPerformedEvent WithStartSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._startSecondsRecipe = configure(_startSecondsRecipe);
        copy._startSeconds       = copy._startSecondsRecipe;

        return copy;
    }

    /// <summary>Pins <c>durationSeconds</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPerformedEvent WithDurationSeconds(double value) {
        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._durationSeconds = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>durationSeconds</c> is drawn from — <c>AnyValidDurationSeconds()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPerformedEvent WithDurationSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPerformedEvent copy = new AnyPerformedEvent(this);

        copy._durationSecondsRecipe = configure(_durationSecondsRecipe);
        copy._durationSeconds       = copy._durationSecondsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PerformedEvent" />.</summary>
    public PerformedEvent Generate() {
        return new PerformedEvent(_staffName.Generate(),
                                  _voiceNumber.Generate(),
                                  _pitch.Generate(),
                                  _velocity.Generate(),
                                  _onset.Generate(),
                                  _length.Generate(),
                                  _startSeconds.Generate(),
                                  _durationSeconds.Generate());
    }

}
