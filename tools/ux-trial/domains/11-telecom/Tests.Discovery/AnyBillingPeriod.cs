// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BillingPeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BillingPeriod" /> values. Drawn as a start and a length in days
///     (1 to 31) rather than two independent bounds: the 31-day limit ties the end to the start.
/// </summary>
public sealed partial class AnyBillingPeriod : IAny<BillingPeriod> {

    private IAny<DateOnly> _start;
    private IAny<int>      _lengthDays;

    private AnyDateOnly _startRecipe;
    private AnyInt32    _lengthDaysRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBillingPeriod()
        : this(start: AnyValidStart(), lengthDays: AnyValidLengthDays()) { }

    private static AnyDateOnly AnyValidStart() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
    }

    private static AnyInt32 AnyValidLengthDays() {
        return Any.Int32().Between(1, 31);
    }

    private AnyBillingPeriod(AnyDateOnly start, AnyInt32 lengthDays) {
        _startRecipe      = start;
        _start            = start;
        _lengthDaysRecipe = lengthDays;
        _lengthDays       = lengthDays;
    }

    private AnyBillingPeriod(AnyBillingPeriod from) {
        _start            = from._start;
        _startRecipe      = from._startRecipe;
        _lengthDays       = from._lengthDays;
        _lengthDaysRecipe = from._lengthDaysRecipe;
    }

    public AnyBillingPeriod WithStart(DateOnly value) {
        AnyBillingPeriod copy = new AnyBillingPeriod(this);

        copy._start = Any.Fixed(value);

        return copy;
    }

    public AnyBillingPeriod WithStart(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBillingPeriod copy = new AnyBillingPeriod(this);

        copy._startRecipe = configure(_startRecipe);
        copy._start       = copy._startRecipe;

        return copy;
    }

    public AnyBillingPeriod WithLengthDays(int value) {
        AnyBillingPeriod copy = new AnyBillingPeriod(this);

        copy._lengthDays = Any.Fixed(value);

        return copy;
    }

    public AnyBillingPeriod WithLengthDays(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBillingPeriod copy = new AnyBillingPeriod(this);

        copy._lengthDaysRecipe = configure(_lengthDaysRecipe);
        copy._lengthDays       = copy._lengthDaysRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BillingPeriod" />.</summary>
    public BillingPeriod Generate() {
        DateOnly start = _start.Generate();

        return new BillingPeriod(start, start.AddDays(_lengthDays.Generate()));
    }

}
