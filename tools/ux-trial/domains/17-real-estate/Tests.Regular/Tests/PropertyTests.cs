namespace RealEstate.Tests.Regular;

public sealed class PropertyTests {

    [Fact]
    public void A_property_exposes_what_it_was_built_with() {
        // Arrange
        CadastralReference         parcel    = new CadastralReference("75056 000 AB 0123");
        Surface                    area      = new Surface(64.5);
        EnergyPerformanceDiagnosis diagnosis = Any.EnergyPerformanceDiagnosis().Generate();
        List<DocumentLink>         documents = Any.ListOf(Any.DocumentLink()).WithCount(3).DistinctBy(document => document.Location).Generate();

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
    public void A_property_needs_a_parcel() {
        // Arrange
        Surface                    anyArea      = Any.Surface().Generate();
        EnergyPerformanceDiagnosis anyDiagnosis = Any.EnergyPerformanceDiagnosis().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Property(null!, PropertyKind.House, anyArea, anyDiagnosis, []));

        // Assert
        Assert.Equal("parcel", exception.ParamName);
    }

    [Fact]
    public void A_property_needs_a_living_area() {
        // Arrange
        CadastralReference         anyParcel    = Any.CadastralReference().Generate();
        EnergyPerformanceDiagnosis anyDiagnosis = Any.EnergyPerformanceDiagnosis().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Property(anyParcel, PropertyKind.House, null!, anyDiagnosis, []));

        // Assert
        Assert.Equal("livingArea", exception.ParamName);
    }

    [Fact]
    public void A_property_needs_a_diagnosis() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        Surface            anyArea   = Any.Surface().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Property(anyParcel, PropertyKind.House, anyArea, null!, []));

        // Assert
        Assert.Equal("diagnosis", exception.ParamName);
    }

    [Fact]
    public void A_property_needs_a_list_of_documents() {
        // Arrange
        CadastralReference         anyParcel    = Any.CadastralReference().Generate();
        Surface                    anyArea      = Any.Surface().Generate();
        EnergyPerformanceDiagnosis anyDiagnosis = Any.EnergyPerformanceDiagnosis().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Property(anyParcel, PropertyKind.House, anyArea, anyDiagnosis, null!));

        // Assert
        Assert.Equal("documents", exception.ParamName);
    }

    [Fact]
    public void A_kind_that_is_not_a_house_an_apartment_or_commercial_is_refused() {
        // Arrange
        CadastralReference         anyParcel     = Any.CadastralReference().Generate();
        Surface                    anyArea       = Any.Surface().Generate();
        EnergyPerformanceDiagnosis anyDiagnosis  = Any.EnergyPerformanceDiagnosis().Generate();
        PropertyKind               anyOtherKind  = (PropertyKind)Any.Int32().Except(1, 2, 3).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Property(anyParcel, anyOtherKind, anyArea, anyDiagnosis, []));

        // Assert
        Assert.Equal("kind", exception.ParamName);
    }

    [Theory]
    [InlineData(PropertyKind.House)]
    [InlineData(PropertyKind.Apartment)]
    [InlineData(PropertyKind.Commercial)]
    public void Every_declared_kind_is_accepted(PropertyKind kind) {
        // Act
        Property property = Any.Property().WithKind(kind).Generate();

        // Assert
        Assert.Equal(kind, property.Kind);
    }

    [Fact]
    public void A_property_holds_up_to_twenty_documents() {
        // Arrange
        List<DocumentLink> twentyDocuments = Any.ListOf(Any.DocumentLink()).WithCount(Property.MaxDocuments).DistinctBy(document => document.Location).Generate();

        // Act
        Property property = Any.Property().WithDocuments(twentyDocuments).Generate();

        // Assert
        Assert.Equal(20, property.Documents.Count);
    }

    [Fact]
    public void More_than_twenty_documents_are_refused() {
        // Arrange
        CadastralReference         anyParcel        = Any.CadastralReference().Generate();
        Surface                    anyArea          = Any.Surface().Generate();
        EnergyPerformanceDiagnosis anyDiagnosis     = Any.EnergyPerformanceDiagnosis().Generate();
        List<DocumentLink>         twentyOneDocuments = Any.ListOf(Any.DocumentLink()).WithCount(Property.MaxDocuments + 1).DistinctBy(document => document.Location).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Property(anyParcel, PropertyKind.House, anyArea, anyDiagnosis, twentyOneDocuments));

        // Assert
        Assert.Equal("documents", exception.ParamName);
    }

    [Fact]
    public void Two_documents_at_the_same_location_are_refused() {
        // Arrange
        Uri          sharedLocation = Any.Uri().Web().UsingHttps().Generate();
        DocumentLink first          = Any.DocumentLink().WithLocation(sharedLocation).WithKind(DocumentKind.TitleDeed).Generate();
        DocumentLink second         = Any.DocumentLink().WithLocation(sharedLocation).WithKind(DocumentKind.FloorPlan).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.Property().WithDocuments([first, second]).Generate());

        // Assert
        Assert.Equal("documents", exception.ParamName);
    }

    [Fact]
    public void The_documents_are_copied_when_the_property_is_built() {
        // Arrange
        List<DocumentLink> documents = Any.ListOf(Any.DocumentLink()).WithCount(2).DistinctBy(document => document.Location).Generate();
        Property           property  = Any.Property().WithDocuments(documents).Generate();
        DocumentLink       anyOther  = Any.DocumentLink().Generate();

        // Act
        documents.Add(anyOther);
        documents.RemoveAt(0);

        // Assert
        Assert.Equal(2, property.Documents.Count);
        Assert.DoesNotContain(anyOther, property.Documents);
    }

    [Fact]
    public void Attaching_a_document_adds_it_to_the_end_of_the_documents() {
        // Arrange
        Property     property = Any.Property().WithDocuments(l => l.WithCount(2)).Generate();
        DocumentLink attached = Any.DocumentLink().Generate();

        // Act
        property.Attach(attached);

        // Assert
        Assert.Equal(3, property.Documents.Count);
        Assert.Same(attached, property.Documents[2]);
    }

    [Fact]
    public void A_null_document_cannot_be_attached() {
        // Arrange
        Property property = Any.Property().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => property.Attach(null!));

        // Assert
        Assert.Equal("document", exception.ParamName);
    }

    [Fact]
    public void A_document_whose_location_is_already_attached_is_refused() {
        // Arrange
        Uri          location = new Uri("https://docs.example.com/deeds/42.pdf");
        DocumentLink existing = Any.DocumentLink().WithLocation(location).WithKind(DocumentKind.TitleDeed).Generate();
        DocumentLink sameSpot = Any.DocumentLink().WithLocation(location).WithKind(DocumentKind.FloorPlan).Generate();
        Property     property = Any.Property().WithDocuments([existing]).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => property.Attach(sameSpot));
        Assert.Single(property.Documents);
    }

    [Fact]
    public void The_same_document_cannot_be_attached_twice() {
        // Arrange
        Property     property = Any.Property().WithDocuments([]).Generate();
        DocumentLink document = Any.DocumentLink().Generate();
        property.Attach(document);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => property.Attach(document));
    }

    [Fact]
    public void A_twentieth_document_can_be_attached() {
        // Arrange
        Property     property = Any.Property().WithDocuments(l => l.WithCount(Property.MaxDocuments - 1)).Generate();
        DocumentLink twentieth = Any.DocumentLink().Generate();

        // Act
        property.Attach(twentieth);

        // Assert
        Assert.Equal(Property.MaxDocuments, property.Documents.Count);
    }

    [Fact]
    public void No_document_can_be_attached_to_a_full_folder() {
        // Arrange
        Property     property = Any.Property().WithDocuments(l => l.WithCount(Property.MaxDocuments)).Generate();
        DocumentLink anyNew   = Any.DocumentLink().Generate();

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => property.Attach(anyNew));

        // Assert
        Assert.Equal(Property.MaxDocuments, property.Documents.Count);
        Assert.Contains("full", exception.Message);
    }

    [Fact]
    public void A_more_recent_diagnosis_replaces_the_current_one() {
        // Arrange
        Property                   property     = Any.Property().WithDiagnosis(d => d.WithIssuedOn(new DateOnly(2023, 1, 10))).Generate();
        EnergyPerformanceDiagnosis newDiagnosis = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2023, 1, 11)).Generate();

        // Act
        property.Rediagnose(newDiagnosis);

        // Assert
        Assert.Same(newDiagnosis, property.Diagnosis);
    }

    [Fact]
    public void A_diagnosis_issued_the_same_day_does_not_replace_the_current_one() {
        // Arrange
        EnergyPerformanceDiagnosis current  = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2023, 1, 10)).Generate();
        EnergyPerformanceDiagnosis sameDay  = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2023, 1, 10)).Generate();
        Property                   property = Any.Property().WithDiagnosis(current).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => property.Rediagnose(sameDay));
        Assert.Same(current, property.Diagnosis);
    }

    [Fact]
    public void An_older_diagnosis_does_not_replace_the_current_one() {
        // Arrange
        EnergyPerformanceDiagnosis current  = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2023, 1, 10)).Generate();
        EnergyPerformanceDiagnosis older    = Any.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2023, 1, 9)).Generate();
        Property                   property = Any.Property().WithDiagnosis(current).Generate();

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => property.Rediagnose(older));

        // Assert
        Assert.Same(current, property.Diagnosis);
        Assert.Contains("more recent", exception.Message);
    }

    [Fact]
    public void A_null_diagnosis_cannot_replace_the_current_one() {
        // Arrange
        Property property = Any.Property().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => property.Rediagnose(null!));

        // Assert
        Assert.Equal("diagnosis", exception.ParamName);
    }

    [Theory]
    // A rating G bans new lets from 2025-01-01, F from 2028-01-01, E from 2034-01-01; A to D are never banned.
    [InlineData(EnergyRating.G, PropertyKind.House, "2024-12-31", true)]
    [InlineData(EnergyRating.G, PropertyKind.House, "2025-01-01", false)]
    [InlineData(EnergyRating.G, PropertyKind.Apartment, "2024-12-31", true)]
    [InlineData(EnergyRating.G, PropertyKind.Apartment, "2025-01-01", false)]
    [InlineData(EnergyRating.F, PropertyKind.House, "2027-12-31", true)]
    [InlineData(EnergyRating.F, PropertyKind.House, "2028-01-01", false)]
    [InlineData(EnergyRating.F, PropertyKind.Apartment, "2025-01-01", true)]
    [InlineData(EnergyRating.E, PropertyKind.House, "2033-12-31", true)]
    [InlineData(EnergyRating.E, PropertyKind.House, "2034-01-01", false)]
    [InlineData(EnergyRating.E, PropertyKind.Apartment, "2030-06-01", true)]
    public void The_climate_and_resilience_calendar_bans_the_worst_ratings_progressively(EnergyRating rating, PropertyKind kind, string day, bool canBeLet) {
        // Arrange: valid from 2024-06-01 to 2034-05-31
        Property property = Any.Property()
                               .WithKind(kind)
                               .WithDiagnosis(d => d.WithRating(rating).WithIssuedOn(new DateOnly(2024, 6, 1)))
                               .Generate();

        // Act
        bool actual = property.CanBeLetOn(DateOnly.Parse(day));

        // Assert
        Assert.Equal(canBeLet, actual);
    }

    [Theory]
    [InlineData(EnergyRating.A)]
    [InlineData(EnergyRating.B)]
    [InlineData(EnergyRating.C)]
    [InlineData(EnergyRating.D)]
    public void A_property_rated_A_to_D_is_never_banned(EnergyRating rating) {
        // Arrange
        Property property = Any.Property()
                               .WithKind(Any.Enum<PropertyKind>().Except(PropertyKind.Commercial).Generate())
                               .WithDiagnosis(d => d.WithRating(rating).WithIssuedOn(new DateOnly(2024, 6, 1)))
                               .Generate();

        // Act & Assert
        Assert.True(property.CanBeLetOn(new DateOnly(2024, 6, 1)));
        Assert.True(property.CanBeLetOn(new DateOnly(2034, 5, 31)));
    }

    [Theory]
    [InlineData(EnergyRating.E)]
    [InlineData(EnergyRating.F)]
    [InlineData(EnergyRating.G)]
    public void Commercial_premises_are_outside_the_calendar(EnergyRating rating) {
        // Arrange
        Property property = Any.Property()
                               .WithKind(PropertyKind.Commercial)
                               .WithDiagnosis(d => d.WithRating(rating).WithIssuedOn(new DateOnly(2024, 6, 1)))
                               .Generate();

        // Act & Assert
        Assert.True(property.CanBeLetOn(new DateOnly(2034, 5, 31)));
    }

    [Theory]
    [InlineData(PropertyKind.House)]
    [InlineData(PropertyKind.Apartment)]
    [InlineData(PropertyKind.Commercial)]
    public void A_property_cannot_be_let_when_its_diagnosis_is_not_valid_that_day(PropertyKind kind) {
        // Arrange: valid from 2024-06-01 up to 2034-05-31
        Property property = Any.Property()
                               .WithKind(kind)
                               .WithDiagnosis(d => d.WithRating(EnergyRating.A).WithIssuedOn(new DateOnly(2024, 6, 1)))
                               .Generate();

        // Act & Assert
        Assert.False(property.CanBeLetOn(new DateOnly(2024, 5, 31)));
        Assert.False(property.CanBeLetOn(new DateOnly(2034, 6, 1)));
    }

    [Theory]
    [InlineData("200000", "80", "2500.00")]
    [InlineData("100000", "3", "33333.33")]
    [InlineData("1.01", "2", "0.50")]       // 0.505: halves round to the even digit
    [InlineData("1.03", "2", "0.52")]       // 0.515
    [InlineData("1.05", "2", "0.52")]       // 0.525
    [InlineData("0.01", "10", "0.00")]
    public void The_price_per_square_meter_is_rounded_to_the_cent_half_to_even(string price, string area, string expected) {
        // Arrange
        Property property = Any.Property().WithLivingArea(new Surface(double.Parse(area, System.Globalization.CultureInfo.InvariantCulture))).Generate();

        // Act
        decimal actual = property.PricePerSquareMeter(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), actual);
    }

    [Fact]
    public void A_price_of_zero_has_no_price_per_square_meter() {
        // Arrange
        Property property = Any.Property().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => property.PricePerSquareMeter(0m));

        // Assert
        Assert.Equal("price", exception.ParamName);
    }

    [Fact]
    public void A_negative_price_has_no_price_per_square_meter() {
        // Arrange
        Property property    = Any.Property().Generate();
        decimal  anyNegative = Any.Decimal().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => property.PricePerSquareMeter(anyNegative));
    }

}
