// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DataLogger --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using AgriTech.Domain;
using JustDummies;

namespace ColdChain.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DataLogger" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDataLogger : IAny<DataLogger> {

    private IAny<SensorId>           _id;
    private IAny<int>                _recordingIntervalMinutes;

    private AnySensorId _idRecipe;
    private AnyInt32    _recordingIntervalMinutesRecipe;
    private AnyDateOnly _calibratedOnRecipe;
    private AnyDateOnly _calibrationDueOnRecipe;

    private DateOnly? _calibratedOnPin;
    private DateOnly? _calibrationDueOnPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDataLogger()
        : this(id:                       AnyValidId(),
               recordingIntervalMinutes: AnyValidRecordingIntervalMinutes(),
               calibratedOn:             AnyValidCalibratedOn(),
               calibrationDueOn:         AnyValidCalibrationDueOn()) { }

    private static AnySensorId AnyValidId() {
        return new AnySensorId();
    }

    private static AnyInt32 AnyValidRecordingIntervalMinutes() {
        return Any.Int32().Positive().LessThanOrEqualTo(60);
    }

    private static AnyDateOnly AnyValidCalibratedOn() {
        return Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2045, 12, 31));
    }

    private static AnyDateOnly AnyValidCalibrationDueOn() {
        return Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2045, 12, 31));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDataLogger(AnySensorId id,
                          AnyInt32    recordingIntervalMinutes,
                          AnyDateOnly calibratedOn,
                          AnyDateOnly calibrationDueOn) {
        _id                              = id;
        _idRecipe                        = id;
        _recordingIntervalMinutes        = recordingIntervalMinutes;
        _recordingIntervalMinutesRecipe  = recordingIntervalMinutes;
        _calibratedOnRecipe              = calibratedOn;
        _calibrationDueOnRecipe          = calibrationDueOn;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDataLogger(AnyDataLogger from) {
        _id                              = from._id;
        _idRecipe                        = from._idRecipe;
        _recordingIntervalMinutes        = from._recordingIntervalMinutes;
        _recordingIntervalMinutesRecipe  = from._recordingIntervalMinutesRecipe;
        _calibratedOnRecipe              = from._calibratedOnRecipe;
        _calibratedOnPin                 = from._calibratedOnPin;
        _calibrationDueOnRecipe          = from._calibrationDueOnRecipe;
        _calibrationDueOnPin             = from._calibrationDueOnPin;
    }

    /// <summary>Draws <c>calibratedOn</c> and <c>calibrationDueOn</c>, <c>calibratedOn</c> always before <c>calibrationDueOn</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (DateOnly CalibratedOn, DateOnly CalibrationDueOn) CalibratedOnAndCalibrationDueOn() {
        if (_calibratedOnPin is { } calibratedOn) { return (calibratedOn, _calibrationDueOnPin ?? _calibrationDueOnRecipe.After(calibratedOn).Generate()); }
        if (_calibrationDueOnPin is { } calibrationDueOn) { return (_calibratedOnRecipe.Before(calibrationDueOn).Generate(), calibrationDueOn); }

        return Any.Pair(_calibratedOnRecipe, _calibrationDueOnRecipe).Having.Item1BeforeItem2().Generate();
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDataLogger WithId(SensorId value) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySensorId, AnySensorId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDataLogger WithId(Func<AnySensorId, AnySensorId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDataLogger copy = new AnyDataLogger(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>recordingIntervalMinutes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDataLogger WithRecordingIntervalMinutes(int value) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._recordingIntervalMinutes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>recordingIntervalMinutes</c> is drawn from — <c>AnyValidRecordingIntervalMinutes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDataLogger WithRecordingIntervalMinutes(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDataLogger copy = new AnyDataLogger(this);

        copy._recordingIntervalMinutesRecipe = configure(_recordingIntervalMinutesRecipe);
        copy._recordingIntervalMinutes       = copy._recordingIntervalMinutesRecipe;

        return copy;
    }

    /// <summary>Pins <c>calibratedOn</c> to a fixed value, which bounds <c>calibrationDueOn</c>; its recipe is kept for a later refinement.</summary>
    public AnyDataLogger WithCalibratedOn(DateOnly value) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._calibratedOnPin                 = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>calibratedOn</c> is drawn from — <c>AnyValidCalibratedOn()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>calibrationDueOn</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDataLogger WithCalibratedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDataLogger copy = new AnyDataLogger(this);

        copy._calibratedOnRecipe              = configure(_calibratedOnRecipe);
        copy._calibratedOnPin                 = null;

        return copy;
    }

    /// <summary>Pins <c>calibrationDueOn</c> to a fixed value, which bounds <c>calibratedOn</c>; its recipe is kept for a later refinement.</summary>
    public AnyDataLogger WithCalibrationDueOn(DateOnly value) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._calibrationDueOnPin             = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>calibrationDueOn</c> is drawn from — <c>AnyValidCalibrationDueOn()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>calibratedOn</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDataLogger WithCalibrationDueOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDataLogger copy = new AnyDataLogger(this);

        copy._calibrationDueOnRecipe          = configure(_calibrationDueOnRecipe);
        copy._calibrationDueOnPin             = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="DataLogger" />.</summary>
    public DataLogger Generate() {
        (DateOnly calibratedOn, DateOnly calibrationDueOn) = CalibratedOnAndCalibrationDueOn();

        // A certificate is valid for at most a year after the calibration: the pair is ordered, this keeps it close.
        if (calibrationDueOn.DayNumber - calibratedOn.DayNumber > 365) {
            if (_calibrationDueOnPin is null) {
                calibrationDueOn = calibratedOn.AddDays(Any.Int32().Between(1, 365).Generate());
            } else if (_calibratedOnPin is null) {
                calibratedOn = calibrationDueOn.AddDays(-Any.Int32().Between(1, 365).Generate());
            }
        }

        return new DataLogger(_id.Generate(),
                              _recordingIntervalMinutes.Generate(),
                              calibratedOn,
                              calibrationDueOn);
    }

}
