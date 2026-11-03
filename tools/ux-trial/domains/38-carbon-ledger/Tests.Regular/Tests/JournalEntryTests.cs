using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class JournalEntryTests {

    private static Account AnAccountOfKind(AccountKind kind, string number) {
        return new AnyAccount().WithKind(kind).WithAccountId(number).Generate();
    }

    private static IReadOnlyCollection<CreditSerial> SomeSerials(int count) {
        return Any.ListOf(new AnyCreditSerial().WithVintage(v => v.LessThanOrEqualTo(RetirementClaim.MinReportingYear)))
                  .WithCount(count)
                  .Distinct()
                  .Generate();
    }

    private static RetirementClaim ClaimForYear(int reportingYear) {
        return new AnyRetirementClaim().WithReportingYear(reportingYear).Generate();
    }

    [Fact]
    public void An_issuance_entry_moves_the_units_from_the_issuance_account_to_a_holding_account() {
        // Arrange
        Account                           issuance = AnAccountOfKind(AccountKind.Issuance, "000001");
        Account                           holding  = AnAccountOfKind(AccountKind.Holding, "000002");
        IReadOnlyCollection<CreditSerial> serials  = SomeSerials(3);
        DateTimeOffset                    anyTime  = Any.DateTimeOffset().Generate();

        // Act
        JournalEntry entry = new JournalEntry(7, anyTime, EntryKind.Issuance, issuance, holding, serials, null);

        // Assert
        Assert.Equal(7L, entry.Sequence);
        Assert.Equal(EntryKind.Issuance, entry.Kind);
        Assert.Same(issuance, entry.Debit);
        Assert.Same(holding, entry.Credit);
        Assert.Equal(3, entry.Quantity);
        Assert.Equal(serials, entry.Serials);
        Assert.Null(entry.Claim);
    }

    [Fact]
    public void A_transfer_entry_goes_from_a_holding_account_to_another() {
        // Arrange
        Account                           from    = AnAccountOfKind(AccountKind.Holding, "000001");
        Account                           to      = AnAccountOfKind(AccountKind.Holding, "000002");
        IReadOnlyCollection<CreditSerial> serials = SomeSerials(2);
        DateTimeOffset                    anyTime = Any.DateTimeOffset().Generate();

        // Act
        JournalEntry entry = new JournalEntry(1, anyTime, EntryKind.Transfer, from, to, serials, null);

        // Assert
        Assert.Equal(2, entry.Quantity);
        Assert.Equal(EntryKind.Transfer, entry.Kind);
    }

    [Fact]
    public void A_retirement_entry_carries_its_claim() {
        // Arrange
        Account                           holding    = AnAccountOfKind(AccountKind.Holding, "000001");
        Account                           retirement = AnAccountOfKind(AccountKind.Retirement, "000002");
        IReadOnlyCollection<CreditSerial> serials    = SomeSerials(2);
        RetirementClaim                   anyClaim   = new AnyRetirementClaim().Generate();
        DateTimeOffset                    anyTime    = Any.DateTimeOffset().Generate();

        // Act
        JournalEntry entry = new JournalEntry(1, anyTime, EntryKind.Retirement, holding, retirement, serials, anyClaim);

        // Assert
        Assert.Same(anyClaim, entry.Claim);
    }

    [Fact]
    public void The_serials_of_an_entry_are_a_snapshot_of_the_collection_it_was_given() {
        // Arrange
        Account            issuance = AnAccountOfKind(AccountKind.Issuance, "000001");
        Account            holding  = AnAccountOfKind(AccountKind.Holding, "000002");
        List<CreditSerial> serials  = SomeSerials(2).ToList();
        DateTimeOffset     anyTime  = Any.DateTimeOffset().Generate();
        JournalEntry       entry    = new JournalEntry(1, anyTime, EntryKind.Issuance, issuance, holding, serials, null);

        // Act
        serials.Clear();

        // Assert
        Assert.Equal(2, entry.Quantity);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void A_sequence_below_one_is_refused(long sequence) {
        // Arrange
        Account                           issuance = AnAccountOfKind(AccountKind.Issuance, "000001");
        Account                           holding  = AnAccountOfKind(AccountKind.Holding, "000002");
        IReadOnlyCollection<CreditSerial> serials  = SomeSerials(1);
        DateTimeOffset                    anyTime  = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new JournalEntry(sequence, anyTime, EntryKind.Issuance, issuance, holding, serials, null));
    }

    [Fact]
    public void An_entry_cannot_debit_and_credit_the_same_account() {
        // Arrange
        Account                           holding = AnAccountOfKind(AccountKind.Holding, "000001");
        IReadOnlyCollection<CreditSerial> serials = SomeSerials(1);
        DateTimeOffset                    anyTime = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, anyTime, EntryKind.Transfer, holding, holding, serials, null));
    }

    [Fact]
    public void An_entry_must_move_at_least_one_unit() {
        // Arrange
        Account        issuance = AnAccountOfKind(AccountKind.Issuance, "000001");
        Account        holding  = AnAccountOfKind(AccountKind.Holding, "000002");
        DateTimeOffset anyTime  = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, anyTime, EntryKind.Issuance, issuance, holding, Array.Empty<CreditSerial>(), null));
    }

    [Fact]
    public void An_entry_cannot_list_a_null_serial() {
        // Arrange
        Account        issuance = AnAccountOfKind(AccountKind.Issuance, "000001");
        Account        holding  = AnAccountOfKind(AccountKind.Holding, "000002");
        CreditSerial   anySerial = new AnyCreditSerial().Generate();
        DateTimeOffset anyTime  = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, anyTime, EntryKind.Issuance, issuance, holding, new CreditSerial[] { anySerial, null! }, null));
    }

    [Fact]
    public void An_entry_cannot_list_the_same_unit_twice() {
        // Arrange
        Account        issuance  = AnAccountOfKind(AccountKind.Issuance, "000001");
        Account        holding   = AnAccountOfKind(AccountKind.Holding, "000002");
        CreditSerial   anySerial = new AnyCreditSerial().Generate();
        CreditSerial   itsTwin   = new CreditSerial(anySerial.Registry, anySerial.ProjectId, anySerial.Vintage, anySerial.UnitNumber);
        DateTimeOffset anyTime   = Any.DateTimeOffset().Generate();

        // Act / Assert: equal by value is the same unit
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, anyTime, EntryKind.Issuance, issuance, holding, new CreditSerial[] { anySerial, itsTwin }, null));
    }

    public static IEnumerable<object[]> AccountKindsThatDoNotMatchTheEntryKind() {
        foreach (EntryKind entry in Enum.GetValues<EntryKind>()) {
            (AccountKind debit, AccountKind credit) expected = entry switch {
                EntryKind.Issuance => (AccountKind.Issuance, AccountKind.Holding),
                EntryKind.Transfer => (AccountKind.Holding, AccountKind.Holding),
                _                  => (AccountKind.Holding, AccountKind.Retirement)
            };
            foreach (AccountKind debit in Enum.GetValues<AccountKind>()) {
                foreach (AccountKind credit in Enum.GetValues<AccountKind>()) {
                    if (debit != expected.debit || credit != expected.credit) {
                        yield return new object[] { entry, debit, credit };
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(AccountKindsThatDoNotMatchTheEntryKind))]
    public void The_account_kinds_must_match_the_entry_kind(EntryKind entryKind, AccountKind debitKind, AccountKind creditKind) {
        // Arrange
        Account                           debit   = AnAccountOfKind(debitKind, "000001");
        Account                           credit  = AnAccountOfKind(creditKind, "000002");
        IReadOnlyCollection<CreditSerial> serials = SomeSerials(1);
        RetirementClaim?                  claim   = entryKind == EntryKind.Retirement ? new AnyRetirementClaim().Generate() : null;
        DateTimeOffset                    anyTime = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, anyTime, entryKind, debit, credit, serials, claim));
    }

    [Fact]
    public void An_undefined_entry_kind_is_refused() {
        // Arrange
        Account                           issuance = AnAccountOfKind(AccountKind.Issuance, "000001");
        Account                           holding  = AnAccountOfKind(AccountKind.Holding, "000002");
        IReadOnlyCollection<CreditSerial> serials  = SomeSerials(1);
        DateTimeOffset                    anyTime  = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new JournalEntry(1, anyTime, (EntryKind)42, issuance, holding, serials, null));
    }

    [Fact]
    public void A_retirement_must_state_its_claim() {
        // Arrange
        Account                           holding    = AnAccountOfKind(AccountKind.Holding, "000001");
        Account                           retirement = AnAccountOfKind(AccountKind.Retirement, "000002");
        IReadOnlyCollection<CreditSerial> serials    = SomeSerials(1);
        DateTimeOffset                    anyTime    = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new JournalEntry(1, anyTime, EntryKind.Retirement, holding, retirement, serials, null));
    }

    [Theory]
    [InlineData(EntryKind.Issuance)]
    [InlineData(EntryKind.Transfer)]
    public void Only_a_retirement_carries_a_claim(EntryKind kind) {
        // Arrange
        Account debit = kind == EntryKind.Issuance
            ? AnAccountOfKind(AccountKind.Issuance, "000001")
            : AnAccountOfKind(AccountKind.Holding, "000001");
        Account                           credit   = AnAccountOfKind(AccountKind.Holding, "000002");
        IReadOnlyCollection<CreditSerial> serials  = SomeSerials(1);
        RetirementClaim                   anyClaim = new AnyRetirementClaim().Generate();
        DateTimeOffset                    anyTime  = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, anyTime, kind, debit, credit, serials, anyClaim));
    }

    [Fact]
    public void A_retirement_cannot_offset_a_reporting_year_before_the_latest_vintage_it_retires() {
        // Arrange
        Account      holding    = AnAccountOfKind(AccountKind.Holding, "000001");
        Account      retirement = AnAccountOfKind(AccountKind.Retirement, "000002");
        CreditSerial oldUnit    = new AnyCreditSerial().WithVintage(2010).Generate();
        CreditSerial newUnit    = new AnyCreditSerial().WithVintage(2022).Generate();
        DateTimeOffset anyTime  = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, anyTime, EntryKind.Retirement, holding, retirement, new CreditSerial[] { oldUnit, newUnit }, ClaimForYear(2021)));

        // Assert
        Assert.Contains("2022", refusal.Message);
        Assert.Contains("2021", refusal.Message);
    }

    [Fact]
    public void A_retirement_may_offset_the_very_year_of_the_latest_vintage() {
        // Arrange
        Account        holding    = AnAccountOfKind(AccountKind.Holding, "000001");
        Account        retirement = AnAccountOfKind(AccountKind.Retirement, "000002");
        CreditSerial   oldUnit    = new AnyCreditSerial().WithVintage(2010).Generate();
        CreditSerial   newUnit    = new AnyCreditSerial().WithVintage(2022).Generate();
        DateTimeOffset anyTime    = Any.DateTimeOffset().Generate();

        // Act
        JournalEntry entry = new JournalEntry(1, anyTime, EntryKind.Retirement, holding, retirement, new CreditSerial[] { oldUnit, newUnit }, ClaimForYear(2022));

        // Assert
        Assert.Equal(2022, entry.Claim!.ReportingYear);
    }

    [Fact]
    public void A_null_account_or_serial_collection_is_refused() {
        // Arrange
        Account                           issuance = AnAccountOfKind(AccountKind.Issuance, "000001");
        Account                           holding  = AnAccountOfKind(AccountKind.Holding, "000002");
        IReadOnlyCollection<CreditSerial> serials  = SomeSerials(1);
        DateTimeOffset                    anyTime  = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new JournalEntry(1, anyTime, EntryKind.Issuance, null!, holding, serials, null));
        Assert.Throws<ArgumentNullException>(() => new JournalEntry(1, anyTime, EntryKind.Issuance, issuance, null!, serials, null));
        Assert.Throws<ArgumentNullException>(() => new JournalEntry(1, anyTime, EntryKind.Issuance, issuance, holding, null!, null));
    }

}
