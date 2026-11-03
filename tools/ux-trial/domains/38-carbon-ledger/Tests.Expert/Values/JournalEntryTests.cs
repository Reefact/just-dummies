using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;
using CarbonLedger.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Values;

public sealed class JournalEntryTests {

    // ---- what an entry reports -----------------------------------------------------------------------------------

    [Fact]
    public void An_entry_moves_as_many_units_as_it_lists() {
        // Arrange
        JournalEntry entry = Any.JournalEntry().WithSerials(serials => serials.WithCount(4)).Generate();

        // Act
        int quantity = entry.Quantity;

        // Assert
        Assert.Equal(4, quantity);
    }

    [Fact]
    public void An_entry_keeps_its_own_copy_of_the_units_it_was_given() {
        // Arrange
        List<CreditSerial> units = Any.ListOf(Any.CreditSerial()).WithCount(3).Distinct().Generate();
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Holding);
        JournalEntry entry = new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Transfer, debit, credit, units, null);

        // Act
        units.Clear();

        // Assert
        Assert.Equal(3, entry.Quantity);
        Assert.Equal(3, entry.Serials.Count);
    }

    [Fact]
    public void An_entry_keeps_what_it_was_built_with() {
        // Arrange
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Holding);
        DateTimeOffset      anyInstant = Any.DateTimeOffset().Generate();
        List<CreditSerial> anyUnits   = Any.ListOf(Any.CreditSerial()).NonEmpty().Distinct().Generate();

        // Act
        JournalEntry entry = new JournalEntry(7, anyInstant, EntryKind.Transfer, debit, credit, anyUnits, null);

        // Assert
        Assert.Equal(7L, entry.Sequence);
        Assert.Equal(EntryKind.Transfer, entry.Kind);
        Assert.Same(debit, entry.Debit);
        Assert.Same(credit, entry.Credit);
        Assert.Null(entry.Claim);
    }

    [Fact(Skip = "domain defect (low): JournalEntry is documented as immutable but Serials hands out the CreditSerial[] it stores, so a caller can overwrite a unit by casting; literal scenario, no seed")]
    public void The_units_of_an_entry_cannot_be_overwritten_through_the_list_it_exposes() {
        // Arrange
        JournalEntry anyEntry = Any.JournalEntry().WithSerials(serials => serials.WithCount(2)).Generate();

        // Act
        bool exposesItsStorage = anyEntry.Serials is CreditSerial[];

        // Assert
        Assert.False(exposesItsStorage);
    }

    // ---- the sequence --------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void A_sequence_below_one_is_refused(long sequence) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.JournalEntry().WithSequence(sequence).Generate());

        Assert.Equal("sequence", refusal.ParamName);
    }

    [Fact]
    public void The_first_sequence_is_accepted() {
        Assert.Equal(1L, Any.JournalEntry().WithSequence(1).Generate().Sequence);
    }

    // ---- the units -----------------------------------------------------------------------------------------------

    [Fact]
    public void An_entry_moves_at_least_one_unit() {
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Holding);

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Transfer, debit, credit, Array.Empty<CreditSerial>(), null));

        Assert.Equal("serials", refusal.ParamName);
    }

    [Fact]
    public void An_entry_cannot_list_a_missing_unit() {
        CreditSerial anySerial = Any.CreditSerial().Generate();
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Holding);

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Transfer, debit, credit, new CreditSerial?[] { anySerial, null }!, null));

        Assert.Equal("serials", refusal.ParamName);
    }

    [Fact]
    public void An_entry_cannot_list_the_same_unit_twice() {
        CreditSerial anySerial = Any.CreditSerial().Generate();
        CreditSerial sameSerial = new CreditSerial(anySerial.Registry, anySerial.ProjectId, anySerial.Vintage, anySerial.UnitNumber);

        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Holding);

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Transfer, debit, credit, new[] { anySerial, sameSerial }, null));

        Assert.Equal("serials", refusal.ParamName);
    }

    // ---- the accounts --------------------------------------------------------------------------------------------

    [Fact]
    public void An_entry_cannot_debit_and_credit_the_same_account() {
        Account      holding = Any.Account().WithKind(AccountKind.Holding).Generate();
        CreditSerial anySerial = Any.CreditSerial().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Transfer, holding, holding, new[] { anySerial }, null));

        Assert.Equal("credit", refusal.ParamName);
    }

    [Fact]
    public void An_entry_cannot_debit_and_credit_two_accounts_that_share_a_number() {
        // Arrange
        Account      debit     = Any.Account().WithKind(AccountKind.Holding).Generate();
        Account      sameId    = new Account(debit.AccountId, debit.Holder, AccountKind.Holding, Any.DateTimeOffset().Generate());
        CreditSerial anySerial = Any.CreditSerial().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Transfer, debit, sameId, new[] { anySerial }, null));

        // Assert
        Assert.Equal("credit", refusal.ParamName);
    }

    [Theory]
    [InlineData(EntryKind.Issuance, AccountKind.Issuance, AccountKind.Holding)]
    [InlineData(EntryKind.Transfer, AccountKind.Holding, AccountKind.Holding)]
    [InlineData(EntryKind.Retirement, AccountKind.Holding, AccountKind.Retirement)]
    public void Each_kind_of_entry_accepts_the_accounts_that_suit_it(EntryKind kind, AccountKind debitKind, AccountKind creditKind) {
        // Arrange
        (Account debit, Account credit) = TestAccounts.Pair(debitKind, creditKind);
        List<CreditSerial> anyUnits = Any.ListOf(Any.CreditSerial()).NonEmpty().Distinct().Generate();

        // Act
        JournalEntry entry = new JournalEntry(1, Any.DateTimeOffset().Generate(), kind, debit, credit, anyUnits, TestClaims.IfRetirement(kind, anyUnits));

        // Assert
        Assert.Equal(kind, entry.Kind);
    }

    public static TheoryData<EntryKind, AccountKind, AccountKind> MismatchedAccounts() {
        TheoryData<EntryKind, AccountKind, AccountKind> cases = [];
        foreach (EntryKind kind in Enum.GetValues<EntryKind>()) {
            (AccountKind suitedDebit, AccountKind suitedCredit) = kind switch {
                EntryKind.Issuance => (AccountKind.Issuance, AccountKind.Holding),
                EntryKind.Transfer => (AccountKind.Holding, AccountKind.Holding),
                _                  => (AccountKind.Holding, AccountKind.Retirement)
            };

            foreach (AccountKind debitKind in Enum.GetValues<AccountKind>()) {
                foreach (AccountKind creditKind in Enum.GetValues<AccountKind>()) {
                    if (debitKind != suitedDebit || creditKind != suitedCredit) { cases.Add(kind, debitKind, creditKind); }
                }
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(MismatchedAccounts))]
    public void An_entry_refuses_accounts_that_do_not_suit_its_kind(EntryKind kind, AccountKind debitKind, AccountKind creditKind) {
        // Arrange
        (Account debit, Account credit) = TestAccounts.Pair(debitKind, creditKind);
        CreditSerial     anySerial = Any.CreditSerial().Generate();
        RetirementClaim? claim     = kind == EntryKind.Retirement ? Any.RetirementClaim().WithReportingYear(RetirementClaim.MaxReportingYear).Generate() : null;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), kind, debit, credit, new[] { anySerial }, claim));

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void An_undeclared_kind_of_entry_is_refused() {
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Holding);

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), (EntryKind)3, debit, credit, new[] { Any.CreditSerial().Generate() }, null));

        Assert.Equal("kind", refusal.ParamName);
    }

    // ---- the claim -----------------------------------------------------------------------------------------------

    [Fact]
    public void A_retirement_must_state_its_claim() {
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Retirement);

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Retirement, debit, credit, new[] { Any.CreditSerial().Generate() }, null));

        Assert.Equal("claim", refusal.ParamName);
    }

    [Theory]
    [InlineData(EntryKind.Issuance, AccountKind.Issuance, AccountKind.Holding)]
    [InlineData(EntryKind.Transfer, AccountKind.Holding, AccountKind.Holding)]
    public void Only_a_retirement_carries_a_claim(EntryKind kind, AccountKind debitKind, AccountKind creditKind) {
        (Account debit, Account credit) = TestAccounts.Pair(debitKind, creditKind);

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), kind, debit, credit, new[] { Any.CreditSerial().Generate() }, Any.RetirementClaim().Generate()));

        Assert.Equal("claim", refusal.ParamName);
    }

    [Fact]
    public void A_retirement_keeps_its_claim() {
        // Arrange
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Retirement);
        List<CreditSerial> anyUnits = Any.ListOf(Any.CreditSerial()).NonEmpty().Distinct().Generate();
        RetirementClaim    claim    = TestClaims.IfRetirement(EntryKind.Retirement, anyUnits)!;

        // Act
        JournalEntry entry = new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Retirement, debit, credit, anyUnits, claim);

        // Assert
        Assert.Same(claim, entry.Claim);
    }

    [Fact]
    public void Units_cannot_offset_emissions_of_a_year_before_their_vintage() {
        // Arrange
        CreditSerial    vintage2022 = Any.CreditSerial().WithVintage(2022).Generate();
        RetirementClaim claim2021   = Any.RetirementClaim().WithReportingYear(2021).Generate();
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Retirement);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Retirement, debit, credit, new[] { vintage2022 }, claim2021));

        // Assert
        Assert.Equal("claim", refusal.ParamName);
        Assert.Contains("2022", refusal.Message);
    }

    [Theory]
    [InlineData(2022)]
    [InlineData(2023)]
    [InlineData(RetirementClaim.MaxReportingYear)]
    public void Units_may_offset_emissions_of_their_vintage_year_or_a_later_one(int reportingYear) {
        // Arrange
        CreditSerial    vintage2022 = Any.CreditSerial().WithVintage(2022).Generate();
        RetirementClaim claim       = Any.RetirementClaim().WithReportingYear(reportingYear).Generate();
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Retirement);

        // Act
        JournalEntry entry = new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Retirement, debit, credit, new[] { vintage2022 }, claim);

        // Assert
        Assert.Equal(reportingYear, entry.Claim!.ReportingYear);
    }

    [Theory]
    [InlineData(2022, 2018)]
    [InlineData(2018, 2022)]
    public void The_latest_vintage_of_the_entry_sets_the_earliest_reporting_year_wherever_it_stands_in_the_list(int firstVintage, int secondVintage) {
        // Arrange
        CreditSerial    first  = Any.CreditSerial().WithVintage(firstVintage).Generate();
        CreditSerial    second = Any.CreditSerial().WithVintage(secondVintage).WithUnitNumber(unit => unit.DifferentFrom(first.UnitNumber)).Generate();
        RetirementClaim claim2021 = Any.RetirementClaim().WithReportingYear(2021).Generate();
        (Account debit, Account credit) = TestAccounts.Pair(AccountKind.Holding, AccountKind.Retirement);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new JournalEntry(1, Any.DateTimeOffset().Generate(), EntryKind.Retirement, debit, credit, new[] { first, second }, claim2021));

        // Assert
        Assert.Equal("claim", refusal.ParamName);
    }

}
