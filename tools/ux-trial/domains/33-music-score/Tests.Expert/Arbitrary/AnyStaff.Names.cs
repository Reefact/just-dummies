// Written by hand, never touched by `any generate Staff --force`.

using JustDummies;

namespace MusicScore.Tests.Expert.Arbitrary;

public sealed partial class AnyStaff {

    /// <summary>Names a staff may carry — the same base the generator draws from, for a test that needs several.</summary>
    public static IAny<string> Names() {
        return AnyValidName();
    }

}
