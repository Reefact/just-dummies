// Written by hand, never touched by `any generate Score --force`.

using JustDummies;

using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

public sealed partial class AnyScore {

    /// <summary>Titles a score may carry — the generator's own base, for a test that builds the score itself.</summary>
    public static IAny<string> Titles() {
        return AnyValidTitle();
    }

    /// <summary>A score of one arbitrary staff whose measures follow <paramref name="shapes" />.</summary>
    public AnyScore WithMeasures(params Func<AnyMeasure, AnyMeasure>[] shapes) {
        return new AnyScore(_title, _tempo, new AnyStaff().WithMeasures(shapes).As(staff => (IReadOnlyList<Staff>)[staff]));
    }

}
