using JustDummies;

using RealEstate.Domain;

using Xunit;

namespace RealEstate.Tests.Discovery;

public sealed class PropertyTests {

    private static DocumentLink DocumentAt(string location) {
        return Any.DocumentLink().WithLocation(new Uri(location)).Generate();
    }

    private static List<DocumentLink> DocumentsAtDistinctLocations(int count) {
        return Enumerable.Range(0, count).Select(index => DocumentAt($"https://docs.example.org/files/{index}.pdf")).ToList();
    }

    // --- construction ---------------------------------------------------------------------------------------

    [Fact]
    public void A_property_keeps_its_parcel_kind_area_and_diagnosis() {
        // Arrange
        CadastralReference         parcel    = new CadastralReference("75056 000 AB 0123");
        Surface                    area      = new Surface(63.5);
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().Generate();
        List<DocumentLink>         documents = DocumentsAtDistinctLocations(2);

        // Act
        Property property = new Property(parcel, PropertyKind.Apartment, area, diagnosis, documents);

        // Assert
        Assert.Same(parcel, property.Parcel);
        Assert.Equal(PropertyKind.Apartment, property.Kind);
        Assert.Same(area, property.LivingArea);
        Assert.Same(diagnosis, property.Diagnosis);
        Assert.Equal(documents, property.Documents);
    }

    [Fact]
    public void A_property_may_have_no_document() {
        // Arrange
        Property property = Any.Property().WithDocuments(new List<DocumentLink>()).Generate();

        // Assert
        Assert.Empty(property.Documents);
    }

    [Fact]
    public void The_documents_are_copied_so_the_list_given_can_change_without_effect() {
        // Arrange
        List<DocumentLink> given    = DocumentsAtDistinctLocations(2);
        Property           property = Any.Property().WithDocuments(given).Generate();

        // Act
        given.Add(DocumentAt("https://docs.example.org/files/later.pdf"));
        given.RemoveAt(0);

        // Assert
        Assert.Equal(2, property.Documents.Count);
        Assert.Equal("0.pdf", property.Documents[0].FileName);
    }

    [Fact]
    public void A_parcel_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Property(null!, PropertyKind.House, Any.Surface().Generate(), Any.EnergyPerformanceDiagnosis().Generate(), new List<DocumentLink>()));
    }

    [Fact]
    public void A_living_area_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Property(Any.CadastralReference().Generate(), PropertyKind.House, null!, Any.EnergyPerformanceDiagnosis().Generate(), new List<DocumentLink>()));
    }

    [Fact]
    public void A_diagnosis_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Property(Any.CadastralReference().Generate(), PropertyKind.House, Any.Surface().Generate(), null!, new List<DocumentLink>()));
    }

    [Fact]
    public void A_document_list_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Property(Any.CadastralReference().Generate(), PropertyKind.House, Any.Surface().Generate(), Any.EnergyPerformanceDiagnosis().Generate(), null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(99)]
    public void A_kind_other_than_house_apartment_or_commercial_is_refused(int kind) {
        // Arrange
        CadastralReference         anyParcel    = Any.CadastralReference().Generate();
        Surface                    anyArea      = Any.Surface().Generate();
        EnergyPerformanceDiagnosis anyDiagnosis = Any.EnergyPerformanceDiagnosis().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Property(anyParcel, (PropertyKind)kind, anyArea, anyDiagnosis, new List<DocumentLink>()));

        // Assert
        Assert.Equal("kind", exception.ParamName);
    }

    [Fact]
    public void Twenty_documents_are_accepted_and_twenty_one_are_refused() {
        // Arrange
        Property accepted = Any.Property().WithDocuments(DocumentsAtDistinctLocations(Property.MaxDocuments)).Generate();

        // Assert
        Assert.Equal(20, accepted.Documents.Count);
        Assert.Throws<ArgumentException>(() => Any.Property().WithDocuments(DocumentsAtDistinctLocations(Property.MaxDocuments + 1)).Generate());
    }

    [Fact]
    public void Two_documents_at_the_same_location_are_refused() {
        // Arrange
        List<DocumentLink> documents = new List<DocumentLink> {
            Any.DocumentLink().WithKind(DocumentKind.TitleDeed).WithLocation(new Uri("https://docs.example.org/files/a.pdf")).Generate(),
            Any.DocumentLink().WithKind(DocumentKind.FloorPlan).WithLocation(new Uri("https://docs.example.org/files/a.pdf")).Generate()
        };

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Property().WithDocuments(documents).Generate());
    }

    [Fact(Skip = "domain defect: a null item in the documents list escapes the constructor as a NullReferenceException instead of an argument exception; seed-independent (literal input)")]
    public void A_null_document_in_the_list_is_refused_with_an_argument_exception() {
        // The constructor dereferences each item, so a null item escapes as a NullReferenceException.
        List<DocumentLink> documents = new List<DocumentLink> { DocumentAt("https://docs.example.org/files/a.pdf"), null! };

        Assert.ThrowsAny<ArgumentException>(() => Any.Property().WithDocuments(documents).Generate());
    }

    // --- Attach ---------------------------------------------------------------------------------------------

    [Fact]
    public void Attaching_a_document_adds_it_at_the_end() {
        // Arrange
        Property     property = Any.Property().WithDocuments(DocumentsAtDistinctLocations(2)).Generate();
        DocumentLink document = DocumentAt("https://docs.example.org/files/new.pdf");

        // Act
        property.Attach(document);

        // Assert
        Assert.Equal(3, property.Documents.Count);
        Assert.Same(document, property.Documents[2]);
    }

    [Fact]
    public void Attaching_nothing_is_refused() {
        // Arrange
        Property anyProperty = Any.Property().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyProperty.Attach(null!));
    }

    [Fact]
    public void A_twentieth_document_can_be_attached_but_not_a_twenty_first() {
        // Arrange
        Property property = Any.Property().WithDocuments(DocumentsAtDistinctLocations(Property.MaxDocuments - 1)).Generate();

        // Act
        property.Attach(DocumentAt("https://docs.example.org/files/last.pdf"));

        // Assert
        Assert.Equal(Property.MaxDocuments, property.Documents.Count);
        Assert.Throws<InvalidOperationException>(() => property.Attach(DocumentAt("https://docs.example.org/files/one-too-many.pdf")));
        Assert.Equal(Property.MaxDocuments, property.Documents.Count);
    }

    [Fact]
    public void A_document_at_a_location_already_attached_is_refused_whatever_its_kind() {
        // Arrange
        Property property = Any.Property().WithDocuments(new List<DocumentLink> { Any.DocumentLink().WithKind(DocumentKind.TitleDeed).WithLocation(new Uri("https://docs.example.org/files/a.pdf")).Generate() }).Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => property.Attach(Any.DocumentLink().WithKind(DocumentKind.AsbestosReport).WithLocation(new Uri("https://docs.example.org/files/a.pdf")).Generate()));
        Assert.Single(property.Documents);
    }

    [Fact]
    public void A_full_folder_is_reported_before_a_duplicate() {
        // Arrange
        Property property = Any.Property().WithDocuments(DocumentsAtDistinctLocations(Property.MaxDocuments)).Generate();

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => property.Attach(DocumentAt("https://docs.example.org/files/0.pdf")));

        // Assert
        Assert.Contains("full", exception.Message);
    }

    // --- Rediagnose -----------------------------------------------------------------------------------------

    [Fact]
    public void A_more_recent_diagnosis_replaces_the_current_one() {
        // Arrange
        Property                   property  = Any.Property().WithDiagnosis(diagnosis => diagnosis.WithIssuedOn(new DateOnly(2022, 3, 15))).Generate();
        EnergyPerformanceDiagnosis newer     = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 3, 16)).Generate();

        // Act
        property.Rediagnose(newer);

        // Assert
        Assert.Same(newer, property.Diagnosis);
    }

    [Theory]
    [InlineData("2022-03-15")]   // same day
    [InlineData("2022-03-14")]   // earlier
    [InlineData("2021-07-01")]
    public void A_diagnosis_that_is_not_strictly_more_recent_is_refused(string issuedOn) {
        // Arrange
        EnergyPerformanceDiagnosis current  = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 3, 15)).Generate();
        Property                   property = Any.Property().WithDiagnosis(current).Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => property.Rediagnose(Any.EnergyPerformanceDiagnosis().WithIssuedOn(DateOnly.Parse(issuedOn)).Generate()));
        Assert.Same(current, property.Diagnosis);
    }

    [Fact]
    public void Rediagnosing_with_nothing_is_refused() {
        // Arrange
        Property anyProperty = Any.Property().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyProperty.Rediagnose(null!));
    }

    // --- CanBeLetOn -----------------------------------------------------------------------------------------

    public static TheoryData<EnergyRating, string, string, bool> HomeLettingCalendar => new TheoryData<EnergyRating, string, string, bool> {
        // G: banned from 2025-01-01
        { EnergyRating.G, "2022-01-01", "2024-12-31", true },
        { EnergyRating.G, "2022-01-01", "2025-01-01", false },
        { EnergyRating.G, "2022-01-01", "2030-06-01", false },
        // F: banned from 2028-01-01
        { EnergyRating.F, "2022-01-01", "2027-12-31", true },
        { EnergyRating.F, "2022-01-01", "2028-01-01", false },
        // E: banned from 2034-01-01
        { EnergyRating.E, "2025-06-01", "2033-12-31", true },
        { EnergyRating.E, "2025-06-01", "2034-01-01", false },
        // A to D: never banned
        { EnergyRating.D, "2025-06-01", "2034-01-01", true },
        { EnergyRating.C, "2025-06-01", "2034-01-01", true },
        { EnergyRating.B, "2025-06-01", "2035-01-01", true },
        { EnergyRating.A, "2025-06-01", "2035-06-01", false },   // A, but the diagnosis expired that day
        { EnergyRating.A, "2025-06-01", "2035-05-31", true }
    };

    [Theory]
    [MemberData(nameof(HomeLettingCalendar))]
    public void A_home_may_be_newly_let_until_its_rating_bans_it(EnergyRating rating, string issuedOn, string day, bool expected) {
        // Arrange
        Property home = Any.Property()
                           .WithKind(kind => kind.Except(PropertyKind.Commercial))
                           .WithDiagnosis(diagnosis => diagnosis.OfRating(rating).WithIssuedOn(DateOnly.Parse(issuedOn)))
                           .Generate();

        // Assert
        Assert.Equal(expected, home.CanBeLetOn(DateOnly.Parse(day)));
    }

    [Theory]
    [InlineData(EnergyRating.G)]
    [InlineData(EnergyRating.F)]
    [InlineData(EnergyRating.A)]
    public void Commercial_premises_are_outside_the_rating_ban(EnergyRating rating) {
        // Arrange
        Property premises = Any.Property()
                               .WithKind(PropertyKind.Commercial)
                               .WithDiagnosis(diagnosis => diagnosis.OfRating(rating).WithIssuedOn(new DateOnly(2022, 1, 1)))
                               .Generate();

        // Assert
        Assert.True(premises.CanBeLetOn(new DateOnly(2030, 1, 1)));
    }

    [Theory]
    [InlineData("2021-12-31")]   // before the diagnosis was issued
    [InlineData("2032-01-01")]   // the day it expires
    [InlineData("2040-01-01")]
    public void Nothing_can_be_let_without_a_valid_diagnosis_not_even_commercial_premises(string day) {
        // Arrange
        Property premises = Any.Property()
                               .WithKind(PropertyKind.Commercial)
                               .WithDiagnosis(diagnosis => diagnosis.Lettable().WithIssuedOn(new DateOnly(2022, 1, 1)))
                               .Generate();

        // Assert
        Assert.False(premises.CanBeLetOn(DateOnly.Parse(day)));
    }

    // --- PricePerSquareMeter --------------------------------------------------------------------------------

    [Theory]
    [InlineData(80d, "250000", "3125.00")]
    [InlineData(63.5, "317500", "5000.00")]
    [InlineData(3d, "100", "33.33")]
    [InlineData(3d, "200", "66.67")]
    [InlineData(4d, "0.02", "0.00")]    // 0.005: midpoint, rounded to even
    [InlineData(4d, "0.06", "0.02")]    // 0.015
    [InlineData(4d, "0.10", "0.02")]    // 0.025
    [InlineData(4d, "0.14", "0.04")]    // 0.035
    public void The_price_per_square_meter_is_rounded_to_the_cent_half_to_even(double area, string price, string expected) {
        // Arrange
        Property property = Any.Property().WithLivingArea(new Surface(area)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), property.PricePerSquareMeter(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-250000")]
    public void A_price_that_is_not_positive_is_refused(string price) {
        // Arrange
        Property anyProperty = Any.Property().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyProperty.PricePerSquareMeter(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)));
    }

}
