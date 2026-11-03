using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IRowElement" /> values: a stitch run or a bracketed repeat, each drawn
///     through the generator its type owns. Written by hand: <c>any</c> does not scaffold an interface.
/// </summary>
public sealed class AnyIRowElement : IAny<IRowElement> {

    private readonly AnyStitchRun    _run;
    private readonly AnyStitchRepeat _repeat;

    public AnyIRowElement()
        : this(new AnyStitchRun(), new AnyStitchRepeat()) { }

    private AnyIRowElement(AnyStitchRun run, AnyStitchRepeat repeat) {
        _run    = run;
        _repeat = repeat;
    }

    /// <summary>Refines the runs this generator draws, and the runs inside its repeats stay as they are.</summary>
    public AnyIRowElement WithRun(Func<AnyStitchRun, AnyStitchRun> configure) {
        return new AnyIRowElement(configure(_run), _repeat);
    }

    public IRowElement Generate() {
        return Any.Boolean().Generate() ? _run.Generate() : _repeat.Generate();
    }

}
