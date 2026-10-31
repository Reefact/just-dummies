using JustDummies;
using JustDummies.Xunit;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Regular.Notes;

namespace MusicScore.Tests.Regular;

public sealed class StructureTests {

    [Fact, Reproducible]
    public void A_voice_that_does_not_fill_its_meter_is_refused() {
        // Arrange
        AnyMeasure anyThreeFourMeasure = Arbitrary.Measure().WithTimeSignature(ThreeFour).WithSingleVoice(AnyNote(HalfNote));

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anyThreeFourMeasure.Generate());

        // Assert
        Assert.Contains("lasts 1/2 but a 3/4 measure lasts 3/4", error.Message);
    }

    [Fact, Reproducible]
    public void Two_voices_of_a_measure_cannot_share_a_number() {
        // Arrange
        Voice anyVoice      = new(1, [AnyNote(Whole)]);
        Voice anyOtherVoice = new(1, [AnyNote(HalfNote), AnyNote(HalfNote)]);
        AnyMeasure anyMeasure = Arbitrary.Measure().WithTimeSignature(FourFour).WithVoices([anyVoice, anyOtherVoice]);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anyMeasure.Generate());

        // Assert
        Assert.Contains("same voice number twice", error.Message);
    }

    [Fact, Reproducible]
    public void A_staff_must_number_its_measures_from_one_in_order() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, Staff.MaxNameLength).Generate();
        Measure[] measures = [AnyBar(1).Generate(), AnyBar(3).Generate()];

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Staff(anyName, measures));

        // Assert
        Assert.Contains("expects measure 2 at position 2 but found measure 3", error.Message);
    }

    [Fact, Reproducible]
    public void Staves_of_a_score_must_have_the_same_number_of_measures() {
        // Arrange
        Staff anyShortStaff = Arbitrary.Staff().WithName("Violin").WithMeasures([AnyBar(1).Generate()]).Generate();
        Staff anyLongStaff  = Arbitrary.Staff().WithName("Cello").WithMeasures([AnyBar(1).Generate(), AnyBar(2).Generate()]).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => AnyScoreOf(anyShortStaff, anyLongStaff));

        // Assert
        Assert.Contains("Staff 'Cello' has 2 measures but staff 'Violin' has 1", error.Message);
    }

    [Fact, Reproducible]
    public void Staves_of_a_score_must_agree_on_the_meter_of_each_measure() {
        // Arrange
        Staff anyFourFourStaff  = Arbitrary.Staff().WithName("Flute").WithMeasures([AnyBar(1).Generate()]).Generate();
        Staff anyThreeFourStaff = Arbitrary.Staff().WithName("Oboe").WithMeasures([AnyBar(1).WithTimeSignature(ThreeFour).Generate()]).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => AnyScoreOf(anyFourFourStaff, anyThreeFourStaff));

        // Assert
        Assert.Contains("Measure 1 is in 3/4 on staff 'Oboe' but in 4/4 on staff 'Flute'", error.Message);
    }

    [Fact, Reproducible]
    public void A_start_repeat_that_is_never_closed_is_refused() {
        // Arrange
        Staff anyStaff = AnyStaffOf(AnyBar(1).WithRepeatStart(true), AnyBar(2));

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => AnyScoreOf(anyStaff));

        // Assert
        Assert.Contains("never closed", error.Message);
    }

    [Fact, Reproducible]
    public void Nested_repeats_are_refused() {
        // Arrange
        Staff anyStaff = AnyStaffOf(AnyBar(1).WithRepeatStart(true), AnyBar(2).WithRepeatStart(true), AnyBar(3).WithRepeatEndTimes(2));

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => AnyScoreOf(anyStaff));

        // Assert
        Assert.Contains("nested repeats are not supported", error.Message);
    }

}
