using JustDummies;
using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Expert.Generators;

/// <summary>
///     A generator of an arbitrary <see cref="IRowElement" />: a stitch run or a bracketed repeat, written by
///     hand because `any` cannot scaffold an interface.
/// </summary>
public sealed class AnyIRowElement : IAny<IRowElement> {

    private readonly AnyStitchRun    _run    = new AnyStitchRun();
    private readonly AnyStitchRepeat _repeat = new AnyStitchRepeat();

    public IRowElement Generate() {
        return Any.Boolean().Generate() ? _run.Generate() : _repeat.Generate();
    }

}
