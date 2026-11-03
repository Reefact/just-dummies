// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShiftWindow --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ShiftWindow" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShiftWindow : IAny<ShiftWindow> {

    private AnyDateTime _startRecipe;
    private AnyTimeSpan _lengthRecipe;

    private DateTime? _startPin;
    private DateTime? _endPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShiftWindow() {
        _startRecipe  = AnyValidStart();
        _lengthRecipe = AnyValidLength();
    }

    private static AnyDateTime AnyValidStart() {
        return Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)).WithGranularity(TimeSpan.FromMinutes(1));
    }

    // The constructor ties end to start (end - start in ]0, 12h]): the end is drawn as start + length, so the tie holds by construction.
    private static AnyTimeSpan AnyValidLength() {
        return Any.TimeSpan().Between(TimeSpan.FromMinutes(1), ShiftWindow.LongestShift).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private AnyShiftWindow(AnyShiftWindow from) {
        _startRecipe  = from._startRecipe;
        _lengthRecipe = from._lengthRecipe;
        _startPin     = from._startPin;
        _endPin       = from._endPin;
    }

    /// <summary>Pins <c>start</c> to a fixed value; the end follows it.</summary>
    public AnyShiftWindow WithStart(DateTime value) {
        AnyShiftWindow copy = new AnyShiftWindow(this);

        copy._startPin = value;
        copy._endPin   = null;

        return copy;
    }

    /// <summary>Refines the recipe <c>start</c> is drawn from.</summary>
    public AnyShiftWindow WithStart(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftWindow copy = new AnyShiftWindow(this);

        copy._startRecipe = configure(_startRecipe);
        copy._startPin    = null;

        return copy;
    }

    /// <summary>Pins <c>end</c> to a fixed value; the start precedes it.</summary>
    public AnyShiftWindow WithEnd(DateTime value) {
        AnyShiftWindow copy = new AnyShiftWindow(this);

        copy._endPin   = value;
        copy._startPin = null;

        return copy;
    }

    /// <summary>Refines the recipe of the shift's length (end - start).</summary>
    public AnyShiftWindow WithLength(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftWindow copy = new AnyShiftWindow(this);

        copy._lengthRecipe = configure(_lengthRecipe);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ShiftWindow" />.</summary>
    public ShiftWindow Generate() {
        TimeSpan length = _lengthRecipe.Generate();

        if (_endPin is { } end) {
            return new ShiftWindow(end - length, end);
        }

        DateTime start = _startPin ?? _startRecipe.Generate();

        return new ShiftWindow(start, start + length);
    }

}
