using JustDummies;

using MusicScore.Domain;
using MusicScore.Tests.Expert.Arbitrary;

using Xunit;

namespace MusicScore.Tests.Expert;

public sealed class ScoreStructureTests {

    private static readonly Func<AnyMeasure, AnyMeasure> AnyBar = measure => measure;

    private static readonly IAny<List<string>> TwoStaffNames = Any.ListOf(AnyStaff.Names()).WithCount(2).Distinct();

    [Fact]
    public void Staves_of_different_lengths_cannot_share_a_score() {
        // Arrange
        string       anyTitle = AnyScore.Titles().Generate();
        Tempo        anyTempo = Any.Tempo().Generate();
        List<string> anyNames = TwoStaffNames.Generate();
        Staff        anyTwoBarStaff   = Any.Staff().WithName(anyNames[0]).WithMeasures(AnyBar, AnyBar).Generate();
        Staff        anyThreeBarStaff = Any.Staff().WithName(anyNames[1]).WithMeasures(AnyBar, AnyBar, AnyBar).Generate();

        // Act
        Exception? refused = Record.Exception(() => new Score(anyTitle, anyTempo, [anyTwoBarStaff, anyThreeBarStaff]));

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Contains("has 3 measures", invalid.Message);
    }

    [Fact]
    public void Staves_that_disagree_on_a_measures_meter_cannot_share_a_score() {
        // Arrange
        string              anyTitle  = AnyScore.Titles().Generate();
        Tempo               anyTempo  = Any.Tempo().Generate();
        List<string>        anyNames  = TwoStaffNames.Generate();
        List<TimeSignature> anyMeters = Any.ListOf(Any.TimeSignature()).WithCount(2).Distinct().Generate();
        Staff anyStaff      = Any.Staff().WithName(anyNames[0]).WithMeasures(m => m.WithTimeSignature(anyMeters[0])).Generate();
        Staff anyOtherStaff = Any.Staff().WithName(anyNames[1]).WithMeasures(m => m.WithTimeSignature(anyMeters[1])).Generate();

        // Act
        Exception? refused = Record.Exception(() => new Score(anyTitle, anyTempo, [anyStaff, anyOtherStaff]));

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Contains($"in {anyMeters[1]} on staff '{anyNames[1]}'", invalid.Message);
    }

    [Fact]
    public void Two_staves_cannot_carry_the_same_name() {
        // Arrange
        string anyTitle = AnyScore.Titles().Generate();
        Tempo  anyTempo = Any.Tempo().Generate();
        string        anyName  = AnyStaff.Names().Generate();
        TimeSignature anyMeter = Any.TimeSignature().Generate();
        AnyStaff      anyPartNamedAlike = Any.Staff().WithName(anyName).WithMeasures(m => m.WithTimeSignature(anyMeter));

        // Act
        Exception? refused = Record.Exception(() => new Score(anyTitle, anyTempo, [anyPartNamedAlike.Generate(), anyPartNamedAlike.Generate()]));

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Equal("staves", invalid.ParamName);
    }

    [Fact]
    public void A_repeat_opened_inside_an_open_repeat_is_refused() {
        // Arrange
        string anyTitle = AnyScore.Titles().Generate();
        Tempo  anyTempo = Any.Tempo().Generate();
        Staff  anyNestedStaff = Any.Staff().WithMeasures(m => m.OpeningRepeat(), m => m.OpeningRepeat(), m => m.ClosingRepeat(2)).Generate();

        // Act
        Exception? refused = Record.Exception(() => new Score(anyTitle, anyTempo, [anyNestedStaff]));

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Contains("nested repeats are not supported", invalid.Message);
    }

    [Fact]
    public void A_repeat_never_closed_is_refused() {
        // Arrange
        string anyTitle = AnyScore.Titles().Generate();
        Tempo  anyTempo = Any.Tempo().Generate();
        Staff  anyOpenStaff = Any.Staff().WithMeasures(AnyBar, m => m.OpeningRepeat(), AnyBar).Generate();

        // Act
        Exception? refused = Record.Exception(() => new Score(anyTitle, anyTempo, [anyOpenStaff]));

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Contains("never closed", invalid.Message);
    }

}
