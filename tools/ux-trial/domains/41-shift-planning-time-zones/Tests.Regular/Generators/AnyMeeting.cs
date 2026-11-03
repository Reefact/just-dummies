// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Meeting --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace ShiftPlanning.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Meeting" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeeting : IAny<Meeting> {

    private IAny<string>                  _title;
    private IAny<DateTimeOffset>          _start;
    private IAny<TimeSpan>                _duration;
    private IAny<IEnumerable<EmployeeId>> _participants;

    private AnyString               _titleRecipe;
    private AnyDateTimeOffset       _startRecipe;
    private AnyTimeSpan             _durationRecipe;
    private AnySequence<EmployeeId> _participantsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeeting()
        : this(title:        AnyValidTitle(),
               start:        AnyValidStart(),
               duration:     AnyValidDuration(),
               participants: AnyValidParticipants()) { }

    private static AnyString AnyValidTitle() {
        return Any.String().NotBlank().WithMaxLength(120);
    }

    private static AnyDateTimeOffset AnyValidStart() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyTimeSpan AnyValidDuration() {
        return Any.TimeSpan().Between(Meeting.MinDuration, Meeting.MaxDuration).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private static AnySequence<EmployeeId> AnyValidParticipants() {
        return Any.SequenceOf(new AnyEmployeeId()).WithCountBetween(2, Meeting.MaxParticipants).Distinct();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMeeting(AnyString               title,
                       AnyDateTimeOffset       start,
                       AnyTimeSpan             duration,
                       AnySequence<EmployeeId> participants) {
        _title              = title;
        _titleRecipe        = title;
        _start              = start;
        _startRecipe        = start;
        _duration           = duration;
        _durationRecipe     = duration;
        _participants       = participants;
        _participantsRecipe = participants;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMeeting(AnyMeeting from) {
        _title              = from._title;
        _titleRecipe        = from._titleRecipe;
        _start              = from._start;
        _startRecipe        = from._startRecipe;
        _duration           = from._duration;
        _durationRecipe     = from._durationRecipe;
        _participants       = from._participants;
        _participantsRecipe = from._participantsRecipe;
    }

    /// <summary>Pins <c>title</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeeting WithTitle(string value) {
        AnyMeeting copy = new AnyMeeting(this);

        copy._title = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>title</c> is drawn from — <c>AnyValidTitle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeeting WithTitle(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeeting copy = new AnyMeeting(this);

        copy._titleRecipe = configure(_titleRecipe);
        copy._title       = copy._titleRecipe;

        return copy;
    }

    /// <summary>Pins <c>start</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeeting WithStart(DateTimeOffset value) {
        AnyMeeting copy = new AnyMeeting(this);

        copy._start = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>start</c> is drawn from — <c>AnyValidStart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeeting WithStart(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeeting copy = new AnyMeeting(this);

        copy._startRecipe = configure(_startRecipe);
        copy._start       = copy._startRecipe;

        return copy;
    }

    /// <summary>Pins <c>duration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeeting WithDuration(TimeSpan value) {
        AnyMeeting copy = new AnyMeeting(this);

        copy._duration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>duration</c> is drawn from — <c>AnyValidDuration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeeting WithDuration(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeeting copy = new AnyMeeting(this);

        copy._durationRecipe = configure(_durationRecipe);
        copy._duration       = copy._durationRecipe;

        return copy;
    }

    /// <summary>Pins <c>participants</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeeting WithParticipants(IEnumerable<EmployeeId> value) {
        AnyMeeting copy = new AnyMeeting(this);

        copy._participants = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>participants</c> is drawn from — <c>AnyValidParticipants()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;EmployeeId&gt;, AnySequence&lt;EmployeeId&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeeting WithParticipants(Func<AnySequence<EmployeeId>, AnySequence<EmployeeId>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeeting copy = new AnyMeeting(this);

        copy._participantsRecipe = configure(_participantsRecipe);
        copy._participants       = copy._participantsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Meeting" />.</summary>
    public Meeting Generate() {
        return new Meeting(_title.Generate(),
                           _start.Generate(),
                           _duration.Generate(),
                           _participants.Generate());
    }

}
