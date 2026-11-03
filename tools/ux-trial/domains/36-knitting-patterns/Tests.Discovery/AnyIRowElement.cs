using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>Draws a stitch run or a bracketed repeat: whatever a row may be written with.</summary>
public sealed class AnyIRowElement : IAny<IRowElement> {

    public IRowElement Generate() {
        return Any.Boolean()
                  .Then(isRun => isRun
                                    ? new AnyStitchRun().As(run => (IRowElement)run)
                                    : new AnyStitchRepeat().As(repeat => (IRowElement)repeat))
                  .Generate();
    }

}
