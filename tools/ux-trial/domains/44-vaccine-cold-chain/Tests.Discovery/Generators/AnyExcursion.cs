// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Excursion --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Excursion" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyExcursion : IAny<Excursion> {

    private IAny<ExcursionKind>            _kind;
    private IAny<TemperatureRange>         _labelledRange;
    private IAny<bool>                     _isOpen;

    private AnyEnum<ExcursionKind> _kindRecipe;
    private AnyTemperatureRange    _labelledRangeRecipe;
    private AnyDateTimeOffset      _startedAtRecipe;
    private AnyDateTimeOffset      _endedAtRecipe;
    private AnyBoolean             _isOpenRecipe;

    private DateTimeOffset? _startedAtPin;
    private DateTimeOffset? _endedAtPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExcursion()
        : this(kind:           AnyValidKind(),
               labelledRange:  AnyValidLabelledRange(),
               startedAt:      AnyValidStartedAt(),
               endedAt:        AnyValidEndedAt(),
               isOpen:         AnyValidIsOpen()) { }

    private static AnyEnum<ExcursionKind> AnyValidKind() {
        return Any.Enum<ExcursionKind>();
    }

    private static AnyTemperatureRange AnyValidLabelledRange() {
        // A vaccine label: inside the 2-8 degree cold chain, wide enough to leave a reading's room either side of it.
        return new AnyTemperatureRange().WithMinCelsius(m => m.Between(2m, 6m)).WithMaxCelsius(m => m.Between(7m, 8m));
    }

    private static AnyDateTimeOffset AnyValidStartedAt() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 12, 31, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyDateTimeOffset AnyValidEndedAt() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 12, 31, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyBoolean AnyValidIsOpen() {
        return Any.Boolean();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyExcursion(AnyEnum<ExcursionKind> kind,
                         AnyTemperatureRange    labelledRange,
                         AnyDateTimeOffset      startedAt,
                         AnyDateTimeOffset      endedAt,
                         AnyBoolean             isOpen) {
        _kind                 = kind;
        _kindRecipe           = kind;
        _labelledRange        = labelledRange;
        _labelledRangeRecipe  = labelledRange;
        _startedAtRecipe      = startedAt;
        _endedAtRecipe        = endedAt;
        _isOpen               = isOpen;
        _isOpenRecipe         = isOpen;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyExcursion(AnyExcursion from) {
        _kind                 = from._kind;
        _kindRecipe           = from._kindRecipe;
        _labelledRange        = from._labelledRange;
        _labelledRangeRecipe  = from._labelledRangeRecipe;
        _startedAtRecipe      = from._startedAtRecipe;
        _startedAtPin         = from._startedAtPin;
        _endedAtRecipe        = from._endedAtRecipe;
        _endedAtPin           = from._endedAtPin;
        _isOpen               = from._isOpen;
        _isOpenRecipe         = from._isOpenRecipe;
    }

    /// <summary>Draws <c>startedAt</c> and <c>endedAt</c>, <c>startedAt</c> never after <c>endedAt</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (DateTimeOffset StartedAt, DateTimeOffset EndedAt) StartedAtAndEndedAt() {
        if (_startedAtPin is { } startedAt) { return (startedAt, _endedAtPin ?? _endedAtRecipe.AfterOrEqualTo(startedAt).Generate()); }
        if (_endedAtPin is { } endedAt) { return (_startedAtRecipe.BeforeOrEqualTo(endedAt).Generate(), endedAt); }

        return Any.Pair(_startedAtRecipe, _endedAtRecipe).Having.Item1BeforeOrEqualToItem2().Generate();
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExcursion WithKind(ExcursionKind value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ExcursionKind&gt;, AnyEnum&lt;ExcursionKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExcursion WithKind(Func<AnyEnum<ExcursionKind>, AnyEnum<ExcursionKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExcursion copy = new AnyExcursion(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>labelledRange</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExcursion WithLabelledRange(TemperatureRange value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._labelledRange = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>labelledRange</c> is drawn from — <c>AnyValidLabelledRange()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperatureRange, AnyTemperatureRange&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExcursion WithLabelledRange(Func<AnyTemperatureRange, AnyTemperatureRange> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExcursion copy = new AnyExcursion(this);

        copy._labelledRangeRecipe = configure(_labelledRangeRecipe);
        copy._labelledRange       = copy._labelledRangeRecipe;

        return copy;
    }

    /// <summary>Pins <c>startedAt</c> to a fixed value, which bounds <c>endedAt</c>; its recipe is kept for a later refinement.</summary>
    public AnyExcursion WithStartedAt(DateTimeOffset value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._startedAtPin        = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>startedAt</c> is drawn from — <c>AnyValidStartedAt()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>endedAt</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExcursion WithStartedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExcursion copy = new AnyExcursion(this);

        copy._startedAtRecipe     = configure(_startedAtRecipe);
        copy._startedAtPin        = null;

        return copy;
    }

    /// <summary>Pins <c>endedAt</c> to a fixed value, which bounds <c>startedAt</c>; its recipe is kept for a later refinement.</summary>
    public AnyExcursion WithEndedAt(DateTimeOffset value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._endedAtPin          = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>endedAt</c> is drawn from — <c>AnyValidEndedAt()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>startedAt</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExcursion WithEndedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExcursion copy = new AnyExcursion(this);

        copy._endedAtRecipe       = configure(_endedAtRecipe);
        copy._endedAtPin          = null;

        return copy;
    }

    /// <summary>Pins <c>isOpen</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExcursion WithIsOpen(bool value) {
        AnyExcursion copy = new AnyExcursion(this);

        copy._isOpen = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>isOpen</c> is drawn from — <c>AnyValidIsOpen()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExcursion WithIsOpen(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExcursion copy = new AnyExcursion(this);

        copy._isOpenRecipe = configure(_isOpenRecipe);
        copy._isOpen       = copy._isOpenRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Excursion" />.</summary>
    public Excursion Generate() {
        (DateTimeOffset startedAt, DateTimeOffset endedAt) = StartedAtAndEndedAt();

        // The extreme lies strictly beyond the label's end on the side of the kind, so it is derived from both.
        ExcursionKind    kind    = _kind.Generate();
        TemperatureRange range   = _labelledRange.Generate();
        decimal          extreme = kind == ExcursionKind.Cold
                                       ? Any.Decimal().Between(LoggerReading.MinCelsius, range.MinCelsius - 0.1m).WithScale(1).Generate()
                                       : Any.Decimal().Between(range.MaxCelsius + 0.1m, LoggerReading.MaxCelsius).WithScale(1).Generate();

        return new Excursion(kind,
                             range,
                             startedAt,
                             endedAt,
                             extreme,
                             _isOpen.Generate());
    }

}
