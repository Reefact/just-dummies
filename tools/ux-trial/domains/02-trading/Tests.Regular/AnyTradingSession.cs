// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TradingSession --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TradingSession" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTradingSession : IAny<TradingSession> {

    private IAny<DateOnly> _date;
    private IAny<TimeOnly> _opensAt;
    private IAny<TimeOnly> _closesAt;

    private AnyDateOnly _dateRecipe;
    private AnyTimeOnly _opensAtRecipe;
    private AnyTimeOnly _closesAtRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTradingSession()
        : this(date:     AnyValidDate(),
               opensAt:  AnyValidOpensAt(),
               closesAt: AnyValidClosesAt()) { }

    private static AnyDateOnly AnyValidDate() {
        // Exchanges do not trade at the weekend: the pool is the weekdays of forty years.
        DateOnly first = new DateOnly(2000, 1, 3);

        return Any.DateOnly().OneOf(Enumerable.Range(0, 365 * 40)
                                              .Select(offset => first.AddDays(offset))
                                              .Where(day => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)));
    }

    private static AnyTimeOnly AnyValidOpensAt() {
        // A session opens in the morning...
        return Any.TimeOnly().Before(new TimeOnly(12, 0));
    }

    private static AnyTimeOnly AnyValidClosesAt() {
        // ... and closes after noon, so it always opens strictly before it closes.
        return Any.TimeOnly().AfterOrEqualTo(new TimeOnly(12, 0));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTradingSession(AnyDateOnly date,
                              AnyTimeOnly opensAt,
                              AnyTimeOnly closesAt) {
        _date           = date;
        _dateRecipe     = date;
        _opensAt        = opensAt;
        _opensAtRecipe  = opensAt;
        _closesAt       = closesAt;
        _closesAtRecipe = closesAt;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTradingSession(AnyTradingSession from) {
        _date           = from._date;
        _dateRecipe     = from._dateRecipe;
        _opensAt        = from._opensAt;
        _opensAtRecipe  = from._opensAtRecipe;
        _closesAt       = from._closesAt;
        _closesAtRecipe = from._closesAtRecipe;
    }

    /// <summary>Pins <c>date</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTradingSession WithDate(DateOnly value) {
        AnyTradingSession copy = new AnyTradingSession(this);

        copy._date = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>date</c> is drawn from — <c>AnyValidDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTradingSession WithDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTradingSession copy = new AnyTradingSession(this);

        copy._dateRecipe = configure(_dateRecipe);
        copy._date       = copy._dateRecipe;

        return copy;
    }

    /// <summary>Pins <c>opensAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTradingSession WithOpensAt(TimeOnly value) {
        AnyTradingSession copy = new AnyTradingSession(this);

        copy._opensAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>opensAt</c> is drawn from — <c>AnyValidOpensAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTradingSession WithOpensAt(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTradingSession copy = new AnyTradingSession(this);

        copy._opensAtRecipe = configure(_opensAtRecipe);
        copy._opensAt       = copy._opensAtRecipe;

        return copy;
    }

    /// <summary>Pins <c>closesAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTradingSession WithClosesAt(TimeOnly value) {
        AnyTradingSession copy = new AnyTradingSession(this);

        copy._closesAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>closesAt</c> is drawn from — <c>AnyValidClosesAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTradingSession WithClosesAt(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTradingSession copy = new AnyTradingSession(this);

        copy._closesAtRecipe = configure(_closesAtRecipe);
        copy._closesAt       = copy._closesAtRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TradingSession" />.</summary>
    public TradingSession Generate() {
        return new TradingSession(_date.Generate(),
                                  _opensAt.Generate(),
                                  _closesAt.Generate());
    }

}
