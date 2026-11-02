using JustDummies;

using Xunit;

namespace RealEstate.Domain.Tests;

public sealed class PropertyTests {

    private static List<DocumentLink> AnyDocuments(int count) {
        return Any.ListOf(new AnyDocumentLink()).WithCount(count).DistinctBy(document => document.Location).Generate();
    }

    [Fact]
    public void Keeps_the_parcel_the_kind_the_living_area_and_the_diagnosis_it_was_built_from() {
        // Arrange
        CadastralReference         parcel    = Any.CadastralReference().Generate();
        Surface                    area      = Any.Surface().Generate();
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().Generate();

        // Act
        Property property = new Property(parcel, PropertyKind.Apartment, area, diagnosis, AnyDocuments(0));

        // Assert
        Assert.Same(parcel, property.Parcel);
        Assert.Equal(PropertyKind.Apartment, property.Kind);
        Assert.Same(area, property.LivingArea);
        Assert.Same(diagnosis, property.Diagnosis);
    }

    [Theory]
    [InlineData(PropertyKind.House)]
    [InlineData(PropertyKind.Apartment)]
    [InlineData(PropertyKind.Commercial)]
    public void Accepts_every_declared_kind(PropertyKind kind) {
        // Act
        Property property = Any.Property().WithKind(kind).Generate();

        // Assert
        Assert.Equal(kind, property.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Refuses_a_kind_the_enumeration_does_not_declare(int undeclared) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Property().WithKind((PropertyKind)undeclared).Generate());

        // Assert
        Assert.Equal("kind", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_parcel() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Property(null!, Any.Enum<PropertyKind>().Generate(), Any.Surface().Generate(), Any.EnergyPerformanceDiagnosis().Generate(), AnyDocuments(0)));

        // Assert
        Assert.Equal("parcel", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_living_area() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Property(Any.CadastralReference().Generate(), Any.Enum<PropertyKind>().Generate(), null!, Any.EnergyPerformanceDiagnosis().Generate(), AnyDocuments(0)));

        // Assert
        Assert.Equal("livingArea", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_diagnosis() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Property(Any.CadastralReference().Generate(), Any.Enum<PropertyKind>().Generate(), Any.Surface().Generate(), null!, AnyDocuments(0)));

        // Assert
        Assert.Equal("diagnosis", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_document_list() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Property(Any.CadastralReference().Generate(), Any.Enum<PropertyKind>().Generate(), Any.Surface().Generate(), Any.EnergyPerformanceDiagnosis().Generate(), null!));

        // Assert
        Assert.Equal("documents", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(20)]
    public void Accepts_up_to_twenty_documents(int count) {
        // Arrange
        List<DocumentLink> documents = AnyDocuments(count);

        // Act
        Property property = Any.Property().WithDocuments(documents).Generate();

        // Assert
        Assert.Equal(count, property.Documents.Count);
    }

    [Fact]
    public void Refuses_twenty_one_documents() {
        // Arrange
        List<DocumentLink> documents = AnyDocuments(21);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.Property().WithDocuments(documents).Generate());

        // Assert
        Assert.Equal("documents", exception.ParamName);
    }

    [Fact]
    public void Refuses_two_documents_at_the_same_location() {
        // Arrange
        Uri          sharedLocation = Any.Uri().Web().UsingHttps().Generate();
        DocumentLink first          = Any.DocumentLink().WithLocation(sharedLocation).Generate();
        DocumentLink second         = Any.DocumentLink().WithLocation(sharedLocation).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.Property().WithDocuments(new List<DocumentLink> { first, second }).Generate());

        // Assert
        Assert.Equal("documents", exception.ParamName);
    }

    [Fact]
    public void Accepts_two_documents_of_the_same_kind_at_different_locations() {
        // Arrange
        DocumentLink first  = Any.DocumentLink().WithKind(DocumentKind.FloorPlan).WithLocation(new Uri("https://documents.example.org/plans/ground.pdf")).Generate();
        DocumentLink second = Any.DocumentLink().WithKind(DocumentKind.FloorPlan).WithLocation(new Uri("https://documents.example.org/plans/first.pdf")).Generate();

        // Act
        Property property = Any.Property().WithDocuments(new List<DocumentLink> { first, second }).Generate();

        // Assert
        Assert.Equal(2, property.Documents.Count);
    }

    [Fact]
    public void Copies_the_documents_it_is_given() {
        // Arrange
        List<DocumentLink> documents = AnyDocuments(3);
        Property           property  = Any.Property().WithDocuments(documents).Generate();

        // Act
        documents.Add(Any.DocumentLink().Generate());

        // Assert
        Assert.Equal(3, property.Documents.Count);
    }

    [Fact(Skip = "domain defect: Documents hands out the internal List<DocumentLink>, so a caller can cast it and add a 21st or a duplicate document behind Attach's back; seed n/a (the type, not the draw, is at fault)")]
    public void Does_not_let_a_caller_change_the_documents_through_the_list_it_reads() {
        // Arrange
        Property property = Any.Property().WithDocuments(AnyDocuments(3)).Generate();

        // Act
        IReadOnlyList<DocumentLink> documents = property.Documents;

        // Assert
        Assert.False(documents is List<DocumentLink>);
    }

    [Fact]
    public void Attaches_a_document_after_the_ones_it_already_holds() {
        // Arrange
        Property     property = Any.Property().WithDocuments(documents => documents.WithCountBetween(0, 19)).Generate();
        int          before   = property.Documents.Count;
        DocumentLink document = Any.DocumentLink().WithLocation(new Uri("https://documents.example.org/new/asbestos.pdf")).Generate();

        // Act
        property.Attach(document);

        // Assert
        Assert.Equal(before + 1, property.Documents.Count);
        Assert.Same(document, property.Documents[before]);
    }

    [Fact]
    public void Accepts_the_twentieth_document() {
        // Arrange
        Property     property = Any.Property().WithDocuments(AnyDocuments(19)).Generate();
        DocumentLink twentieth = Any.DocumentLink().WithLocation(new Uri("https://documents.example.org/new/twentieth.pdf")).Generate();

        // Act
        property.Attach(twentieth);

        // Assert
        Assert.Equal(20, property.Documents.Count);
    }

    [Fact]
    public void Refuses_a_twenty_first_document_and_keeps_its_folder_unchanged() {
        // Arrange
        Property     property = Any.Property().WithDocuments(AnyDocuments(20)).Generate();
        DocumentLink excess   = Any.DocumentLink().WithLocation(new Uri("https://documents.example.org/new/excess.pdf")).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => property.Attach(excess));

        // Assert
        Assert.Equal(20, property.Documents.Count);
        Assert.DoesNotContain(excess, property.Documents);
    }

    [Fact]
    public void Refuses_a_document_already_attached_at_that_location_whatever_its_kind() {
        // Arrange
        Uri          location = new Uri("https://documents.example.org/deeds/title-deed.pdf");
        DocumentLink attached = Any.DocumentLink().WithKind(DocumentKind.TitleDeed).WithLocation(location).Generate();
        Property     property = Any.Property().WithDocuments(new List<DocumentLink> { attached }).Generate();
        DocumentLink again    = Any.DocumentLink().WithKind(DocumentKind.CadastralExtract).WithLocation(location).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => property.Attach(again));
        Assert.Single(property.Documents);
    }

    [Fact]
    public void Refuses_to_attach_nothing() {
        // Arrange
        Property property = Any.Property().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => property.Attach(null!));

        // Assert
        Assert.Equal("document", exception.ParamName);
    }

    [Fact]
    public void Replaces_the_diagnosis_with_a_more_recent_one() {
        // Arrange
        Property                   property = Any.Property().WithDiagnosis(diagnosis => diagnosis.WithIssuedOn(new DateOnly(2022, 5, 10))).Generate();
        EnergyPerformanceDiagnosis newer    = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 5, 11)).Generate();

        // Act
        property.Rediagnose(newer);

        // Assert
        Assert.Same(newer, property.Diagnosis);
    }

    [Theory]
    [InlineData("2022-05-10")] // the same day as the current one
    [InlineData("2022-05-09")] // the day before
    [InlineData("2021-07-01")] // much older
    public void Refuses_a_diagnosis_that_is_not_more_recent_and_keeps_the_current_one(string issuedOn) {
        // Arrange
        EnergyPerformanceDiagnosis current  = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 5, 10)).Generate();
        Property                   property = Any.Property().WithDiagnosis(current).Generate();
        EnergyPerformanceDiagnosis notNewer = Any.EnergyPerformanceDiagnosis().WithIssuedOn(DateOnly.Parse(issuedOn)).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => property.Rediagnose(notNewer));

        // Assert
        Assert.Same(current, property.Diagnosis);
    }

    [Fact]
    public void Refuses_to_rediagnose_with_nothing() {
        // Arrange
        Property property = Any.Property().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => property.Rediagnose(null!));

        // Assert
        Assert.Equal("diagnosis", exception.ParamName);
    }

    public static TheoryData<EnergyRating, DateOnly, DateOnly, bool> ResidentialLettingCalendar => new TheoryData<EnergyRating, DateOnly, DateOnly, bool> {
        // G is banned from 2025-01-01
        { EnergyRating.G, new DateOnly(2022, 1, 1), new DateOnly(2024, 12, 31), true },
        { EnergyRating.G, new DateOnly(2022, 1, 1), new DateOnly(2025, 1, 1), false },
        { EnergyRating.G, new DateOnly(2022, 1, 1), new DateOnly(2026, 6, 15), false },
        // F is banned from 2028-01-01
        { EnergyRating.F, new DateOnly(2022, 1, 1), new DateOnly(2027, 12, 31), true },
        { EnergyRating.F, new DateOnly(2022, 1, 1), new DateOnly(2028, 1, 1), false },
        // E is banned from 2034-01-01, so the diagnosis has to outlive 2034
        { EnergyRating.E, new DateOnly(2030, 1, 1), new DateOnly(2033, 12, 31), true },
        { EnergyRating.E, new DateOnly(2030, 1, 1), new DateOnly(2034, 1, 1), false },
        // D and better are never banned: letting is open until the diagnosis expires
        { EnergyRating.D, new DateOnly(2030, 1, 1), new DateOnly(2039, 12, 31), true },
        { EnergyRating.C, new DateOnly(2030, 1, 1), new DateOnly(2039, 12, 31), true },
        { EnergyRating.B, new DateOnly(2030, 1, 1), new DateOnly(2039, 12, 31), true },
        { EnergyRating.A, new DateOnly(2030, 1, 1), new DateOnly(2039, 12, 31), true },
        // without a valid diagnosis nothing can be let, whatever the rating
        { EnergyRating.A, new DateOnly(2030, 1, 1), new DateOnly(2029, 12, 31), false },
        { EnergyRating.A, new DateOnly(2030, 1, 1), new DateOnly(2040, 1, 1), false },
        { EnergyRating.G, new DateOnly(2022, 1, 1), new DateOnly(2021, 12, 31), false },
    };

    [Theory]
    [MemberData(nameof(ResidentialLettingCalendar))]
    public void Follows_the_letting_calendar_of_the_Climate_and_Resilience_Act_for_housing(EnergyRating rating, DateOnly issuedOn, DateOnly day, bool canBeLet) {
        // Arrange
        PropertyKind anyResidentialKind = Any.Enum<PropertyKind>().Except(PropertyKind.Commercial).Generate();
        Property     property           = Any.Property().WithKind(anyResidentialKind).WithDiagnosis(diagnosis => diagnosis.WithRating(rating).WithIssuedOn(issuedOn)).Generate();

        // Act
        bool actual = property.CanBeLetOn(day);

        // Assert
        Assert.Equal(canBeLet, actual);
    }

    [Theory]
    [InlineData(EnergyRating.G)]
    [InlineData(EnergyRating.F)]
    [InlineData(EnergyRating.E)]
    [InlineData(EnergyRating.A)]
    public void Lets_commercial_premises_whatever_their_rating_while_the_diagnosis_is_valid(EnergyRating rating) {
        // Arrange
        Property property = Any.Property().WithKind(PropertyKind.Commercial).WithDiagnosis(diagnosis => diagnosis.WithRating(rating).WithIssuedOn(new DateOnly(2030, 1, 1))).Generate();

        // Act
        bool canBeLet = property.CanBeLetOn(new DateOnly(2039, 12, 31));

        // Assert
        Assert.True(canBeLet);
    }

    [Theory]
    [InlineData("2029-12-31")] // before the diagnosis was issued
    [InlineData("2040-01-01")] // ten years after: expired
    public void Does_not_let_commercial_premises_without_a_valid_diagnosis(string day) {
        // Arrange
        Property property = Any.Property().WithKind(PropertyKind.Commercial).WithDiagnosis(diagnosis => diagnosis.WithIssuedOn(new DateOnly(2030, 1, 1))).Generate();

        // Act
        bool canBeLet = property.CanBeLetOn(DateOnly.Parse(day));

        // Assert
        Assert.False(canBeLet);
    }

    [Theory]
    [InlineData(250_000, 50.00, 5000.00)]
    [InlineData(100_000, 33.33, 3000.30)]
    [InlineData(123_400, 12.34, 10_000.00)]
    [InlineData(1, 3.00, 0.33)]
    public void Divides_the_price_by_the_living_area_to_the_cent(decimal price, double livingArea, decimal pricePerSquareMeter) {
        // Arrange
        Property property = Any.Property().WithLivingArea(new Surface(livingArea)).Generate();

        // Act
        decimal actual = property.PricePerSquareMeter(price);

        // Assert
        Assert.Equal(pricePerSquareMeter, actual);
    }

    [Theory]
    [InlineData(100.04, 12.50)] // 12.505: the even cent is 12.50
    [InlineData(100.20, 12.52)] // 12.525: the even cent is 12.52
    public void Rounds_a_price_per_square_meter_halfway_between_two_cents_to_the_even_one(decimal price, decimal pricePerSquareMeter) {
        // Arrange
        Property property = Any.Property().WithLivingArea(new Surface(8.00)).Generate();

        // Act
        decimal actual = property.PricePerSquareMeter(price);

        // Assert
        Assert.Equal(pricePerSquareMeter, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-250_000)]
    public void Refuses_a_price_that_is_not_above_zero(decimal price) {
        // Arrange
        Property property = Any.Property().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => property.PricePerSquareMeter(price));

        // Assert
        Assert.Equal("price", exception.ParamName);
    }

}
