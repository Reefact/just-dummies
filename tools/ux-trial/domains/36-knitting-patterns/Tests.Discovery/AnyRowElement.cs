using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     Written by hand: <c>any</c> cannot scaffold the <see cref="IRowElement" /> interface, and the library
///     offers no "one of these generators", so a drawn boolean picks a stitch run or a bracketed repeat.
/// </summary>
public sealed class AnyRowElement : IAny<IRowElement> {

    private static readonly IAny<bool>         AnyIsRepeat = Any.Boolean();
    private static readonly IAny<StitchRun>    AnyRun      = new AnyStitchRun();
    private static readonly IAny<StitchRepeat> AnyRepeat   = new AnyStitchRepeat();

    public IRowElement Generate() {
        return AnyIsRepeat.Generate() ? AnyRepeat.Generate() : AnyRun.Generate();
    }

}
