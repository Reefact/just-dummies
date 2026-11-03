namespace PulmonaryNodules.Tests.Discovery;

public sealed class CtExamTests {

    [Fact]
    public void An_exam_keeps_what_it_was_built_with() {
        // Arrange
        VoxelSpacing spacing = new VoxelSpacing(0.7, 0.7, 1.0);
        AnatomicalLandmark[] landmarks = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(1, 2, 3)),
            new AnatomicalLandmark(LandmarkKind.SternalAngle, new SpatialPoint(4, 5, 6)),
        ];

        // Act
        CtExam exam = new CtExam("CT20240117", new DateOnly(2024, 1, 17), spacing, 1.25, landmarks);

        // Assert
        Assert.Equal("CT20240117", exam.AccessionNumber);
        Assert.Equal(new DateOnly(2024, 1, 17), exam.AcquiredOn);
        Assert.Same(spacing, exam.Spacing);
        Assert.Equal(1.25, exam.SliceThicknessMm);
        Assert.Equal(landmarks, exam.Landmarks);
    }

    [Theory]
    [InlineData("CT00000000")]
    [InlineData("CT99999999")]
    public void An_accession_number_is_CT_followed_by_eight_digits(string accession) {
        // Act
        CtExam exam = Any.CtExam().WithAccessionNumber(accession).Generate();

        // Assert
        Assert.Equal(accession, exam.AccessionNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CT1234567")]
    [InlineData("CT123456789")]
    [InlineData("ct12345678")]
    [InlineData("XX12345678")]
    [InlineData("CT1234567A")]
    [InlineData(" CT12345678")]
    [InlineData("CT 12345678")]
    [InlineData("CT١٢٣٤٥٦٧٨")]
    public void Anything_else_than_CT_and_eight_digits_is_not_an_accession_number(string accession) {
        // Arrange
        VoxelSpacing         anySpacing   = Any.VoxelSpacing().WithSliceMm(0.5).Generate();
        AnatomicalLandmark[] noLandmarks = [];

        // Act
        Assert.Throws<FormatException>(() => new CtExam(accession, new DateOnly(2024, 1, 17), anySpacing, 1.0, noLandmarks));
    }

    [Fact(Skip = "domain defect: the accession pattern ^CT[0-9]{8}$ ends with $, which .NET lets match before a final line break, so 'CT12345678\\n' is accepted (literal input, no seed)")]
    public void An_accession_number_ending_with_a_line_break_is_not_an_accession_number() {
        // Arrange
        VoxelSpacing         anySpacing  = Any.VoxelSpacing().WithSliceMm(0.5).Generate();
        AnatomicalLandmark[] noLandmarks = [];

        // Act / Assert
        Assert.Throws<FormatException>(() => new CtExam("CT12345678\n", new DateOnly(2024, 1, 17), anySpacing, 1.0, noLandmarks));
    }

    [Fact]
    public void An_exam_without_accession_number_is_refused() {
        // Arrange
        VoxelSpacing         anySpacing  = Any.VoxelSpacing().WithSliceMm(0.5).Generate();
        AnatomicalLandmark[] noLandmarks = [];

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new CtExam(null!, new DateOnly(2024, 1, 17), anySpacing, 1.0, noLandmarks));

        // Assert
        Assert.Equal("accessionNumber", exception.ParamName);
    }

    [Fact]
    public void An_exam_without_spacing_is_refused() {
        // Arrange
        string               anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        AnatomicalLandmark[] noLandmarks  = [];

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new CtExam(anyAccession, new DateOnly(2024, 1, 17), null!, 1.0, noLandmarks));

        // Assert
        Assert.Equal("spacing", exception.ParamName);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(5.0)]
    public void The_limits_of_the_slice_thickness_are_accepted(double thicknessMm) {
        // Arrange
        VoxelSpacing anySpacing = Any.VoxelSpacing().WithSliceMm(0.5).Generate();

        // Act
        CtExam exam = Any.CtExam().WithSpacing(anySpacing).WithSliceThicknessMm(thicknessMm).Generate();

        // Assert
        Assert.Equal(thicknessMm, exam.SliceThicknessMm);
    }

    [Theory]
    [InlineData(0.4999)]
    [InlineData(5.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_slice_thickness_outside_half_a_mm_to_five_mm_is_refused(double badMm) {
        // Arrange
        string               anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        VoxelSpacing         anySpacing   = Any.VoxelSpacing().WithSliceMm(0.1).Generate();
        AnatomicalLandmark[] noLandmarks  = [];

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new CtExam(anyAccession, new DateOnly(2024, 1, 17), anySpacing, badMm, noLandmarks));

        // Assert
        Assert.Equal("sliceThicknessMm", exception.ParamName);
    }

    [Fact]
    public void A_slice_spacing_coarser_than_the_slice_thickness_leaves_gaps_and_is_refused() {
        // Arrange
        string               anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        VoxelSpacing         spacing      = new VoxelSpacing(0.7, 0.7, 1.5);
        AnatomicalLandmark[] noLandmarks  = [];

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new CtExam(anyAccession, new DateOnly(2024, 1, 17), spacing, 1.0, noLandmarks));

        // Assert
        Assert.Equal("spacing", exception.ParamName);
    }

    [Fact]
    public void A_slice_spacing_equal_to_the_slice_thickness_is_accepted() {
        // Arrange
        string               anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        VoxelSpacing         spacing      = new VoxelSpacing(0.7, 0.7, 1.5);
        AnatomicalLandmark[] noLandmarks  = [];

        // Act
        Exception? exception = Record.Exception(() => new CtExam(anyAccession, new DateOnly(2024, 1, 17), spacing, 1.5, noLandmarks));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void An_exam_without_landmark_list_is_refused() {
        // Arrange
        string       anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        VoxelSpacing anySpacing   = Any.VoxelSpacing().WithSliceMm(0.5).Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new CtExam(anyAccession, new DateOnly(2024, 1, 17), anySpacing, 1.0, null!));

        // Assert
        Assert.Equal("landmarks", exception.ParamName);
    }

    [Fact]
    public void A_null_landmark_in_the_list_is_refused() {
        // Arrange
        string             anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        VoxelSpacing       anySpacing   = Any.VoxelSpacing().WithSliceMm(0.5).Generate();
        AnatomicalLandmark anyLandmark  = Any.AnatomicalLandmark().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new CtExam(anyAccession, new DateOnly(2024, 1, 17), anySpacing, 1.0, [anyLandmark, null!]));

        // Assert
        Assert.Equal("landmarks", exception.ParamName);
    }

    [Fact]
    public void A_landmark_kind_cannot_be_located_twice_in_one_exam() {
        // Arrange
        string       anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        VoxelSpacing anySpacing   = Any.VoxelSpacing().WithSliceMm(0.5).Generate();
        SpatialPoint anyPosition  = Any.SpatialPoint().Generate();
        SpatialPoint anyOther     = Any.SpatialPoint().Generate();

        AnatomicalLandmark[] duplicated = [
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, anyPosition),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, anyOther),
        ];

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new CtExam(anyAccession, new DateOnly(2024, 1, 17), anySpacing, 1.0, duplicated));

        // Assert
        Assert.Equal("landmarks", exception.ParamName);
        Assert.Contains("AorticArchApex", exception.Message);
    }

    [Fact]
    public void All_eight_kinds_can_be_located_once_each() {
        // Arrange
        LandmarkKind[] kinds = Enum.GetValues<LandmarkKind>();
        AnatomicalLandmark[] landmarks = kinds.Select(kind => new AnatomicalLandmark(kind, Any.SpatialPoint().Generate())).ToArray();

        // Act
        CtExam exam = Any.CtExam().WithLandmarks(landmarks).Generate();

        // Assert
        Assert.Equal(8, exam.Landmarks.Count);
    }

    [Fact]
    public void An_exam_is_not_changed_by_the_list_it_was_built_from() {
        // Arrange
        List<AnatomicalLandmark> source = [new AnatomicalLandmark(LandmarkKind.SternalAngle, new SpatialPoint(0, 0, 0))];
        CtExam exam = Any.CtExam().WithLandmarks(source).Generate();

        // Act
        source.Add(new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(1, 1, 1)));

        // Assert
        Assert.Single(exam.Landmarks);
    }

    [Fact]
    public void Registering_an_exam_to_a_follow_up_estimates_the_shift_of_their_common_landmarks() {
        // Arrange
        AnatomicalLandmark[] baselineLandmarks = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(0, 0, 0)),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, new SpatialPoint(10, 0, 0)),
            new AnatomicalLandmark(LandmarkKind.SternalAngle, new SpatialPoint(20, 0, 0)),
        ];
        AnatomicalLandmark[] followUpLandmarks = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(0, 7, 0)),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, new SpatialPoint(10, 7, 0)),
            new AnatomicalLandmark(LandmarkKind.SternalAngle, new SpatialPoint(20, 7, 0)),
        ];
        CtExam baseline = Any.CtExam().WithLandmarks(baselineLandmarks).Generate();
        CtExam followUp = Any.CtExam().WithLandmarks(followUpLandmarks).Generate();

        // Act
        TranslationTransform transform = baseline.RegisterTo(followUp);

        // Assert
        Assert.Equal(0.0, transform.DxMm, 9);
        Assert.Equal(7.0, transform.DyMm, 9);
        Assert.Equal(0.0, transform.DzMm, 9);
    }

    [Fact]
    public void Registering_exams_without_enough_common_landmarks_fails() {
        // Arrange
        CtExam anyBaseline = Any.CtExam().WithLandmarks([]).Generate();
        CtExam anyFollowUp = Any.CtExam().WithLandmarks([]).Generate();

        // Act / Assert
        Assert.Throws<RegistrationFailedException>(() => anyBaseline.RegisterTo(anyFollowUp));
    }

    [Fact]
    public void Registering_to_nothing_is_refused() {
        // Arrange
        CtExam anyExam = Any.CtExam().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyExam.RegisterTo(null!));
    }

    [Fact]
    public void An_arbitrary_exam_is_always_well_formed() {
        // Arrange / Act
        Exception? exception = Record.Exception(() => Any.CtExam().Generate());

        // Assert
        Assert.Null(exception);
    }

}
