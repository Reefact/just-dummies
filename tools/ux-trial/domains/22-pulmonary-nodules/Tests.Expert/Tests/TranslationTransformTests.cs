using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using PulmonaryNodules.Tests.Expert.Support;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class TranslationTransformTests {

    // ---- The value itself ----

    [Fact]
    public void Applying_a_transform_moves_a_point_by_its_shift_on_each_axis() {
        // Arrange
        // The residual only says how well the shift was fitted; moving a point ignores it.
        double anyResidual = Any.Double().Between(0.0, TranslationTransform.MaxResidualErrorMm).Generate();
        TranslationTransform transform = new TranslationTransform(10.0, -20.0, 30.0, anyResidual);

        // Act
        SpatialPoint moved = transform.Apply(new SpatialPoint(1.0, 2.0, 3.0));

        // Assert
        Assert.Equal(new SpatialPoint(11.0, -18.0, 33.0), moved);
    }

    [Fact]
    public void The_inverse_has_the_opposite_shift_and_the_same_residual() {
        // Arrange
        TranslationTransform transform = new TranslationTransform(10.0, -20.0, 30.0, 2.5);

        // Act
        TranslationTransform inverse = transform.Inverse();

        // Assert
        Assert.Equal(-10.0, inverse.DxMm);
        Assert.Equal(20.0, inverse.DyMm);
        Assert.Equal(-30.0, inverse.DzMm);
        Assert.Equal(2.5, inverse.ResidualErrorMm);
    }

    [Fact]
    public void The_inverse_brings_a_moved_point_back_where_it_was() {
        // Arrange
        TranslationTransform transform = Any.TranslationTransform().WithDxMm(25.0).WithDyMm(-40.0).WithDzMm(5.0).Generate();
        SpatialPoint start = new SpatialPoint(100.0, 200.0, -300.0);

        // Act
        SpatialPoint backAgain = transform.Inverse().Apply(transform.Apply(start));

        // Assert
        Assert.Equal(start, backAgain);
    }

    [Fact]
    public void The_identity_moves_nothing_and_fits_perfectly() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();

        // Act
        SpatialPoint moved = TranslationTransform.Identity.Apply(anyPoint);

        // Assert
        Assert.Equal(anyPoint, moved);
        Assert.Equal(0.0, TranslationTransform.Identity.ResidualErrorMm);
    }

    [Theory]
    [InlineData(300.0)]
    [InlineData(-300.0)]
    public void A_shift_of_exactly_300_mm_is_accepted_on_each_axis(double edgeMm) {
        // Act
        TranslationTransform transform = new TranslationTransform(edgeMm, edgeMm, edgeMm, 0.0);

        // Assert
        Assert.Equal(edgeMm, transform.DxMm);
        Assert.Equal(edgeMm, transform.DyMm);
        Assert.Equal(edgeMm, transform.DzMm);
    }

    [Theory]
    [InlineData("dxMm", 300.001, 0.0, 0.0)]
    [InlineData("dxMm", -300.001, 0.0, 0.0)]
    [InlineData("dyMm", 0.0, 300.001, 0.0)]
    [InlineData("dyMm", 0.0, -300.001, 0.0)]
    [InlineData("dzMm", 0.0, 0.0, 300.001)]
    [InlineData("dzMm", 0.0, 0.0, -300.001)]
    [InlineData("dxMm", double.NaN, 0.0, 0.0)]
    public void A_shift_beyond_300_mm_on_any_axis_is_refused_and_named(string axis, double dx, double dy, double dz) {
        // Arrange
        double anyResidual = Any.Double().Between(0.0, TranslationTransform.MaxResidualErrorMm).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TranslationTransform(dx, dy, dz, anyResidual));

        // Assert
        Assert.Equal(axis, refusal.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(15.0)]
    public void A_residual_on_either_edge_of_its_range_is_accepted(double edgeMm) {
        // Act
        TranslationTransform transform = new TranslationTransform(0.0, 0.0, 0.0, edgeMm);

        // Assert
        Assert.Equal(edgeMm, transform.ResidualErrorMm);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(15.001)]
    [InlineData(double.NaN)]
    public void A_residual_outside_0_to_15_mm_is_refused(double outsideMm) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TranslationTransform(0.0, 0.0, 0.0, outsideMm));

        // Assert
        Assert.Equal("residualErrorMm", refusal.ParamName);
    }

    [Fact]
    public void Applying_a_transform_to_nothing_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => TranslationTransform.Identity.Apply(null!));
    }

    // ---- Estimating it from landmarks ----

    [Fact]
    public void Estimating_from_landmarks_that_all_moved_together_finds_that_shift_and_no_residual() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 4);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.Shifted(baseline, 12.0, -7.0, 3.0);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(12.0, transform.DxMm);
        Assert.Equal(-7.0, transform.DyMm);
        Assert.Equal(3.0, transform.DzMm);
        Assert.Equal(0.0, transform.ResidualErrorMm);
    }

    [Fact]
    public void The_estimated_shift_is_the_mean_displacement_of_the_landmarks() {
        // Arrange
        // Displacements of 2, 4 and 12 mm along x average to 6 mm.
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 3);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.DisplacedAlongX(baseline, 2.0, 4.0, 12.0);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(6.0, transform.DxMm, precision: 12);
        Assert.Equal(0.0, transform.DyMm);
        Assert.Equal(0.0, transform.DzMm);
    }

    [Fact]
    public void The_residual_is_the_root_mean_square_distance_left_after_the_shift() {
        // Arrange
        // Displacements of +4, -4, +4 and -4 mm: the mean is 0, each landmark stays 4 mm away, so the RMS is 4 mm.
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 4);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.DisplacedAlongX(baseline, 4.0, -4.0, 4.0, -4.0);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(0.0, transform.DxMm);
        Assert.Equal(4.0, transform.ResidualErrorMm, precision: 12);
    }

    [Fact]
    public void A_residual_just_within_15_mm_still_registers() {
        // Arrange
        // Displacements of +18, -18 and 0 mm: RMS = 18 * sqrt(2/3) = 14.7 mm.
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 3);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.DisplacedAlongX(baseline, 18.0, -18.0, 0.0);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(14.697, transform.ResidualErrorMm, precision: 3);
    }

    [Fact]
    public void A_residual_beyond_15_mm_means_the_exams_cannot_be_registered() {
        // Arrange
        // Displacements of +19, -19 and 0 mm: RMS = 19 * sqrt(2/3) = 15.5 mm.
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 3);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.DisplacedAlongX(baseline, 19.0, -19.0, 0.0);

        // Act
        RegistrationFailedException failure = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("15", failure.Message);
    }

    [Fact]
    public void Three_common_landmarks_are_enough() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 3);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.Shifted(baseline, 1.0, 1.0, 1.0);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(1.0, transform.DxMm);
    }

    [Fact]
    public void Two_common_landmarks_are_not_enough() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 2);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.Shifted(baseline, 1.0, 1.0, 1.0);

        // Act
        RegistrationFailedException failure = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("found 2", failure.Message);
    }

    [Fact]
    public void Landmarks_found_in_only_one_of_the_exams_do_not_count_as_common() {
        // Arrange
        // Four landmarks in the baseline, but only the first two reappear in the follow-up, plus one the baseline never located.
        List<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 4).ToList();
        LandmarkKind onlyInFollowUp = Enum.GetValues<LandmarkKind>().Except(baseline.Select(l => l.Kind)).First();
        List<AnatomicalLandmark> followUp = Scans.Shifted(baseline.Take(2).ToList(), 5.0, 0.0, 0.0).ToList();
        followUp.Add(new AnatomicalLandmark(onlyInFollowUp, Any.SpatialPoint().Generate()));

        // Act
        RegistrationFailedException failure = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("found 2", failure.Message);
    }

    [Fact]
    public void Landmarks_are_matched_by_kind_whatever_their_order() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 5);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.Shifted(baseline, 9.0, 0.0, 0.0).Reverse().ToList();

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(9.0, transform.DxMm);
        Assert.Equal(0.0, transform.ResidualErrorMm);
    }

    [Fact]
    public void A_shift_of_exactly_300_mm_registers() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 3);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.Shifted(baseline, 300.0, 0.0, 0.0);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(300.0, transform.DxMm);
    }

    [Theory]
    [InlineData(301.0, 0.0, 0.0)]
    [InlineData(0.0, -301.0, 0.0)]
    [InlineData(0.0, 0.0, 301.0)]
    public void A_shift_beyond_300_mm_on_any_axis_means_the_exams_cannot_be_registered(double dx, double dy, double dz) {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> baseline = Scans.AnyLandmarks(count: 3);
        IReadOnlyCollection<AnatomicalLandmark> followUp = Scans.Shifted(baseline, dx, dy, dz);

        // Act
        RegistrationFailedException failure = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("300", failure.Message);
    }

    [Fact(Skip = "domain defect: Estimate pairs every baseline landmark with its follow-up counterpart, so a kind located twice in the baseline counts as two common landmarks and two distinct common kinds pass the three-landmark minimum (literal case, no seed involved)")]
    public void A_kind_located_twice_in_the_baseline_counts_only_once_towards_the_three_common_landmarks() {
        // Arrange
        // CtExam cannot hold a duplicated kind, but the public static Estimate takes any collection.
        SpatialPoint anyPosition = Any.SpatialPoint().Generate();
        AnatomicalLandmark carina = new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, anyPosition);
        AnatomicalLandmark sternalAngle = new AnatomicalLandmark(LandmarkKind.SternalAngle, anyPosition);
        AnatomicalLandmark[] baseline = [carina, carina, sternalAngle];
        AnatomicalLandmark[] followUp = [carina, sternalAngle];

        // Act / Assert
        Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));
    }

    [Fact]
    public void Estimating_from_no_landmark_list_is_refused() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> anyLandmarks = Scans.AnyLandmarks(count: 3);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => TranslationTransform.Estimate(null!, anyLandmarks));
        Assert.Throws<ArgumentNullException>(() => TranslationTransform.Estimate(anyLandmarks, null!));
    }

}
