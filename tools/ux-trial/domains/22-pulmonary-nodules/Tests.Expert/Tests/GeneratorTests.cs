using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

/// <summary>
///     The generators are test infrastructure with rules of their own (a segmentation that fits its spacing, an exam whose
///     slices leave no gap, landmarks of distinct kinds). These tests hold them to the domain: a generator that drew a
///     value the domain refuses would make every test using it fail on some seeds, far from the cause.
/// </summary>
public sealed class GeneratorTests {

    private const int Draws = 300;

    private static void AssertEveryDrawIsAccepted<T>(IAny<T> generator) {
        List<T> draws = Any.ListOf(generator).WithCount(Draws).Generate();

        Assert.Equal(Draws, draws.Count);
    }

    [Fact]
    public void Spatial_points_are_always_inside_the_field_of_view() {
        AssertEveryDrawIsAccepted(Any.SpatialPoint());
    }

    [Fact]
    public void Voxel_spacings_are_always_inside_their_ranges() {
        AssertEveryDrawIsAccepted(Any.VoxelSpacing());
    }

    [Fact]
    public void Landmarks_are_always_well_formed() {
        AssertEveryDrawIsAccepted(Any.AnatomicalLandmark());
    }

    [Fact]
    public void Transforms_are_always_inside_their_ranges() {
        AssertEveryDrawIsAccepted(Any.TranslationTransform());
    }

    [Fact]
    public void Measured_volumes_always_have_an_uncertainty_below_their_volume() {
        AssertEveryDrawIsAccepted(Any.MeasuredVolume());
    }

    [Fact]
    public void Segmentations_always_fit_their_spacing_in_a_nodule() {
        AssertEveryDrawIsAccepted(Any.Segmentation());
    }

    [Fact]
    public void Volume_changes_are_always_between_valid_measurements() {
        AssertEveryDrawIsAccepted(Any.VolumeChange());
    }

    [Fact]
    public void Exams_always_have_a_valid_accession_gap_free_slices_and_distinct_landmarks() {
        AssertEveryDrawIsAccepted(Any.CtExam());
    }

    [Fact]
    public void Observations_always_carry_a_segmentation_drawn_on_their_exams_spacing() {
        AssertEveryDrawIsAccepted(Any.NoduleObservation());
    }

    [Fact]
    public void Tracked_nodules_are_always_well_formed() {
        AssertEveryDrawIsAccepted(Any.TrackedNodule());
    }

    [Fact]
    public void Exams_pinned_to_a_spacing_or_a_thickness_still_leave_no_gap() {
        // Arrange
        VoxelSpacing coarse = Any.VoxelSpacing().WithSliceMm(4.0).Generate();

        // Act
        List<CtExam> thicker = Any.ListOf(Any.CtExam().WithSpacing(coarse)).WithCount(Draws).Generate();
        List<CtExam> thin = Any.ListOf(Any.CtExam().WithSliceThicknessMm(0.5)).WithCount(Draws).Generate();

        // Assert
        Assert.All(thicker, exam => Assert.True(exam.SliceThicknessMm >= 4.0));
        Assert.All(thin, exam => Assert.True(exam.Spacing.SliceMm <= 0.5));
    }

}
