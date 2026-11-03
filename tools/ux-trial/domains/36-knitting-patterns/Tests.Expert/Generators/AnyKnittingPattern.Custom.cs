using JustDummies;

namespace KnittingPatterns.Tests.Expert.Generators;

public sealed partial class AnyKnittingPattern {

    /// <summary>The valid base of the title, for a test that needs a title on its own.</summary>
    public static AnyString ValidTitle() {
        return AnyValidTitle();
    }

}
