// Written by hand: `any generate RowInstruction` named this generator for the IRowElement
// parameter, but `any generate IRowElement` refuses an interface. An element is either a run
// or a bracketed repeat.

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>A generator of arbitrary row elements: a stitch run or a bracketed repeat.</summary>
public sealed class AnyIRowElement : IAny<IRowElement> {

    private static readonly IAny<IRowElement> Element =
        Any.Combine(Any.Boolean(), new AnyStitchRun(), new AnyStitchRepeat(),
                    (isRepeat, run, repeat) => isRepeat ? (IRowElement)repeat : run);

    public IRowElement Generate() {
        return Element.Generate();
    }

}
