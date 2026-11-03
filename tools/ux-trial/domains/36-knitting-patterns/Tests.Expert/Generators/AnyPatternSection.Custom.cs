using JustDummies;

namespace KnittingPatterns.Tests.Expert.Generators;

public sealed partial class AnyPatternSection {

    /// <summary>The valid base of the name, for a test that needs a section name on its own (AnyValidName stays private).</summary>
    public static AnyString ValidName() {
        return AnyValidName();
    }

}
