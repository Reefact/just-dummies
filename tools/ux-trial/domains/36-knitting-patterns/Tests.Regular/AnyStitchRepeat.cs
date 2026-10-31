// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StitchRepeat --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StitchRepeat" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStitchRepeat : IAny<StitchRepeat> {

    private readonly IAny<IReadOnlyList<StitchRun>> _runs;
    private readonly IAny<int>                      _times;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStitchRepeat()
        : this(runs:  AnyValidRuns(),
               times: AnyValidTimes()) { }

    private static AnyList<StitchRun> AnyValidRuns() {
        // A repeat expands to at most StitchRepeat.MaxOperations: up to 4 runs of up to 10 stitches,
        // worked at most 250 times, stays within 10 000 by construction.
        return Any.ListOf(new AnyStitchRun().WithCount(c => c.LessThanOrEqualTo(10))).WithCountBetween(1, 4);
    }

    private static AnyInt32 AnyValidTimes() {
        return Any.Int32().Between(2, 250);
    }

    private AnyStitchRepeat(IAny<IReadOnlyList<StitchRun>> runs,
                            IAny<int>                      times) {
        _runs  = runs;
        _times = times;
    }

    /// <summary>Pins <c>runs</c> to a fixed value.</summary>
    public AnyStitchRepeat WithRuns(IReadOnlyList<StitchRun> value) {
        return new AnyStitchRepeat(new FixedValue<IReadOnlyList<StitchRun>>(value), _times);
    }

    /// <summary>Draws <c>runs</c> from <c>AnyValidRuns()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStitchRepeat WithRuns(Func<AnyList<StitchRun>, AnyList<StitchRun>> configure) {
        return new AnyStitchRepeat(configure(AnyValidRuns()), _times);
    }

    /// <summary>Pins <c>times</c> to a fixed value.</summary>
    public AnyStitchRepeat WithTimes(int value) {
        return new AnyStitchRepeat(_runs, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>times</c> from <c>AnyValidTimes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStitchRepeat WithTimes(Func<AnyInt32, AnyInt32> configure) {
        return new AnyStitchRepeat(_runs, configure(AnyValidTimes()));
    }

    /// <summary>Produces one arbitrary <see cref="StitchRepeat" />.</summary>
    public StitchRepeat Generate() {
        return new StitchRepeat(_runs.Generate(),
                                _times.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
