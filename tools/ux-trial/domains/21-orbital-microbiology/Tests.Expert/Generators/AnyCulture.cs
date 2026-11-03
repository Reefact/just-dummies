// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Culture --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="Culture" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCulture : IAny<Culture> {

    private IAny<CultureId>              _id;
    private IAny<ExperimentalConditions> _conditions;
    private IAny<DateTimeOffset>         _startedAtUtc;

    private AnyCultureId              _idRecipe;
    private AnyExperimentalConditions _conditionsRecipe;
    private AnyDateTimeOffset         _startedAtUtcRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCulture()
        : this(id:           AnyValidId(),
               conditions:   AnyValidConditions(),
               startedAtUtc: AnyValidStartedAtUtc()) { }

    private static AnyCultureId AnyValidId() {
        return new AnyCultureId();
    }

    private static AnyExperimentalConditions AnyValidConditions() {
        return new AnyExperimentalConditions();
    }

    private static AnyDateTimeOffset AnyValidStartedAtUtc() {
        // Station time is UTC (Any.DateTimeOffset() keeps offset +00:00); the span keeps CurrentTimeUtc clear of the type's ends.
        return Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                            new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCulture(AnyCultureId              id,
                       AnyExperimentalConditions conditions,
                       AnyDateTimeOffset         startedAtUtc) {
        _id                 = id;
        _idRecipe           = id;
        _conditions         = conditions;
        _conditionsRecipe   = conditions;
        _startedAtUtc       = startedAtUtc;
        _startedAtUtcRecipe = startedAtUtc;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCulture(AnyCulture from) {
        _id                 = from._id;
        _idRecipe           = from._idRecipe;
        _conditions         = from._conditions;
        _conditionsRecipe   = from._conditionsRecipe;
        _startedAtUtc       = from._startedAtUtc;
        _startedAtUtcRecipe = from._startedAtUtcRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCulture WithId(CultureId value) {
        AnyCulture copy = new AnyCulture(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCultureId, AnyCultureId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCulture WithId(Func<AnyCultureId, AnyCultureId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCulture copy = new AnyCulture(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>conditions</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCulture WithConditions(ExperimentalConditions value) {
        AnyCulture copy = new AnyCulture(this);

        copy._conditions = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>conditions</c> is drawn from — <c>AnyValidConditions()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyExperimentalConditions, AnyExperimentalConditions&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCulture WithConditions(Func<AnyExperimentalConditions, AnyExperimentalConditions> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCulture copy = new AnyCulture(this);

        copy._conditionsRecipe = configure(_conditionsRecipe);
        copy._conditions       = copy._conditionsRecipe;

        return copy;
    }

    /// <summary>Pins <c>startedAtUtc</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCulture WithStartedAtUtc(DateTimeOffset value) {
        AnyCulture copy = new AnyCulture(this);

        copy._startedAtUtc = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>startedAtUtc</c> is drawn from — <c>AnyValidStartedAtUtc()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCulture WithStartedAtUtc(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCulture copy = new AnyCulture(this);

        copy._startedAtUtcRecipe = configure(_startedAtUtcRecipe);
        copy._startedAtUtc       = copy._startedAtUtcRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Culture" />.</summary>
    public Culture Generate() {
        return new Culture(_id.Generate(),
                           _conditions.Generate(),
                           _startedAtUtc.Generate());
    }

}
