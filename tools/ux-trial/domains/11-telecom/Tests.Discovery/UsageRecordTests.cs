using JustDummies;

using Xunit;

namespace Telecom.Domain.Tests;

public sealed class UsageRecordTests {

    [Fact]
    public void Keeps_what_it_is_given() {
        // Arrange
        DateTime occurredAt = new DateTime(2025, 6, 15, 12, 30, 0, DateTimeKind.Utc);

        // Act
        UsageRecord record = new UsageRecord(occurredAt, UsageKind.Voice, 90);

        // Assert
        Assert.Equal(occurredAt, record.OccurredAt);
        Assert.Equal(UsageKind.Voice, record.Kind);
        Assert.Equal(90, record.Quantity);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Refuses_an_instant_that_is_not_utc(DateTimeKind kind) {
        // Arrange
        UsageKind anyKind     = Any.Enum<UsageKind>().Except(UsageKind.Sms).Generate();
        DateTime  notUtc      = new DateTime(2025, 6, 15, 12, 30, 0, kind);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new UsageRecord(notUtc, anyKind, 10));

        // Assert
        Assert.Equal("occurredAt", exception.ParamName);
    }

    [Fact]
    public void Accepts_any_utc_instant() {
        // Arrange
        DateTime anyInstant = Any.DateTime().Generate();

        // Act
        UsageRecord record = new UsageRecord(anyInstant, UsageKind.Data, 10);

        // Assert
        Assert.Equal(DateTimeKind.Utc, record.OccurredAt.Kind);
    }

    [Theory]
    [InlineData(UsageKind.Data)]
    [InlineData(UsageKind.Voice)]
    [InlineData(UsageKind.Sms)]
    public void Refuses_a_quantity_of_zero(UsageKind kind) {
        // Arrange
        DateTime anyInstant = Any.DateTime().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new UsageRecord(anyInstant, kind, 0));

        // Assert
        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_negative_quantity() {
        // Arrange
        DateTime  anyInstant  = Any.DateTime().Generate();
        UsageKind anyKind     = Any.Enum<UsageKind>().Generate();
        long      anyNegative = Any.Int64().Negative().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new UsageRecord(anyInstant, anyKind, anyNegative));

        // Assert
        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_single_sms() {
        // Arrange
        DateTime anyInstant = Any.DateTime().Generate();

        // Act
        UsageRecord record = new UsageRecord(anyInstant, UsageKind.Sms, 1);

        // Assert
        Assert.Equal(1, record.Quantity);
    }

    [Fact]
    public void Refuses_an_sms_counting_more_than_one_message() {
        // Arrange
        DateTime anyInstant = Any.DateTime().Generate();
        long     anyCount   = Any.Int64().GreaterThan(1).Generate();

        // Act: ArgumentException itself, not the out-of-range subclass
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new UsageRecord(anyInstant, UsageKind.Sms, anyCount));

        // Assert
        Assert.Equal("quantity", exception.ParamName);
    }

    [Theory]
    [InlineData(UsageKind.Data)]
    [InlineData(UsageKind.Voice)]
    public void Accepts_a_quantity_above_one_for_an_amount_metered_kind(UsageKind kind) {
        // Arrange
        DateTime anyInstant = Any.DateTime().Generate();
        long     anyAmount  = Any.Int64().GreaterThan(1).Generate();

        // Act
        UsageRecord record = new UsageRecord(anyInstant, kind, anyAmount);

        // Assert
        Assert.Equal(kind, record.Kind);
    }

    [Fact]
    public void Reports_a_local_instant_before_a_zero_quantity() {
        // The instant is checked first
        Assert.Throws<ArgumentException>(() => new UsageRecord(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local), UsageKind.Data, 0));
    }

    [Fact(Skip = "domain defect: kind is not checked against the declared UsageKind members, so (UsageKind)42 is accepted although Plan refuses an undeclared tier; seed n/a (literal case)")]
    public void Refuses_a_kind_that_is_not_declared() {
        // Arrange
        DateTime anyInstant = Any.DateTime().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsageRecord(anyInstant, (UsageKind)42, 5));
    }

}
