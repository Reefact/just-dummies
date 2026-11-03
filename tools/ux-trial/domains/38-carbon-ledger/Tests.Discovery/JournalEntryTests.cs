using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;
using CarbonLedger.Tests.Discovery.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class JournalEntryTests {

    private static readonly DateTimeOffset RecordedAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Holder FrenchHolder = new Holder("ACME01", "Acme Carbon", "FR");

    private static readonly Account Registry = new Account("FR-000001", FrenchHolder, AccountKind.Issuance, RecordedAt.AddYears(-1));
    private static readonly Account Alice    = new Account("FR-000002", FrenchHolder, AccountKind.Holding, RecordedAt.AddYears(-1));
    private static readonly Account Bob      = new Account("FR-000003", FrenchHolder, AccountKind.Holding, RecordedAt.AddYears(-1));
    private static readonly Account Offsets  = new Account("FR-000004", FrenchHolder, AccountKind.Retirement, RecordedAt.AddYears(-1));

    public static TheoryData<EntryKind, AccountKind, AccountKind> EveryKindAndAccountKindCombination() {
        TheoryData<EntryKind, AccountKind, AccountKind> data = new TheoryData<EntryKind, AccountKind, AccountKind>();
        foreach (EntryKind kind in Enum.GetValues<EntryKind>()) {
            foreach (AccountKind debit in Enum.GetValues<AccountKind>()) {
                foreach (AccountKind credit in Enum.GetValues<AccountKind>()) {
                    data.Add(kind, debit, credit);
                }
            }
        }

        return data;
    }

    private static (AccountKind Debit, AccountKind Credit) AccountKindsOf(EntryKind kind) {
        return kind switch {
            EntryKind.Issuance => (AccountKind.Issuance, AccountKind.Holding),
            EntryKind.Transfer => (AccountKind.Holding, AccountKind.Holding),
            _                  => (AccountKind.Holding, AccountKind.Retirement)
        };
    }

    private static List<CreditSerial> DistinctSerials(int count) {
        return Any.ListOf(Any.CreditSerial()).WithCount(count).Distinct().Generate();
    }

    private static RetirementClaim? ClaimFor(EntryKind kind, IEnumerable<CreditSerial> serials) {
        return kind == EntryKind.Retirement ? LedgerWorld.ClaimCovering(serials) : null;
    }

    [Fact]
    public void An_issuance_entry_exposes_what_it_was_built_from() {
        // Arrange
        List<CreditSerial> serials = DistinctSerials(3);

        // Act
        JournalEntry entry = new JournalEntry(7, RecordedAt, EntryKind.Issuance, Registry, Alice, serials, null);

        // Assert
        Assert.Equal(7, entry.Sequence);
        Assert.Equal(RecordedAt, entry.RecordedAt);
        Assert.Equal(EntryKind.Issuance, entry.Kind);
        Assert.Same(Registry, entry.Debit);
        Assert.Same(Alice, entry.Credit);
        Assert.Equal(serials, entry.Serials);
        Assert.Null(entry.Claim);
    }

    [Fact]
    public void A_retirement_entry_carries_its_claim() {
        // Arrange
        List<CreditSerial> serials = DistinctSerials(2);
        RetirementClaim    claim   = new RetirementClaim("Acme Carbon", "fleet", 2100);

        // Act
        JournalEntry entry = new JournalEntry(1, RecordedAt, EntryKind.Retirement, Alice, Offsets, serials, claim);

        // Assert
        Assert.Same(claim, entry.Claim);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(25)]
    public void The_quantity_of_an_entry_is_the_number_of_units_it_lists(int unitCount) {
        // Arrange
        List<CreditSerial> serials = DistinctSerials(unitCount);

        // Act
        JournalEntry entry = new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, Bob, serials, null);

        // Assert
        Assert.Equal(unitCount, entry.Quantity);
    }

    [Fact]
    public void An_entry_keeps_a_snapshot_of_the_serials_it_was_given() {
        // Arrange
        List<CreditSerial> serials  = DistinctSerials(2);
        CreditSerial       original = serials[0];
        JournalEntry       entry    = new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, Bob, serials, null);

        // Act
        serials.Clear();

        // Assert
        Assert.Equal(2, entry.Quantity);
        Assert.Equal(original, entry.Serials[0]);
    }

    [Theory]
    [MemberData(nameof(EveryKindAndAccountKindCombination))]
    public void An_entry_is_accepted_exactly_when_the_account_kinds_match_its_entry_kind(EntryKind kind, AccountKind debitKind, AccountKind creditKind) {
        // Arrange
        Account            debit   = Accounts.Of(debitKind);
        Account            credit  = Accounts.OtherThan(debit, creditKind);
        List<CreditSerial> serials = DistinctSerials(2);
        RetirementClaim?   claim   = ClaimFor(kind, serials);

        // Act
        Func<JournalEntry> construct = () => new JournalEntry(1, RecordedAt, kind, debit, credit, serials, claim);

        // Assert
        if ((debitKind, creditKind) == AccountKindsOf(kind)) {
            Assert.Equal(kind, construct().Kind);
        } else {
            ArgumentException refusal = Assert.Throws<ArgumentException>(construct);
            Assert.Contains(kind.ToString(), refusal.Message);
        }
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void A_positive_sequence_is_accepted(long sequence) {
        List<CreditSerial> serials = DistinctSerials(1);

        JournalEntry entry = new JournalEntry(sequence, RecordedAt, EntryKind.Transfer, Alice, Bob, serials, null);

        Assert.Equal(sequence, entry.Sequence);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void A_sequence_below_one_is_refused(long sequence) {
        List<CreditSerial> serials = DistinctSerials(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new JournalEntry(sequence, RecordedAt, EntryKind.Transfer, Alice, Bob, serials, null));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    public void An_entry_kind_that_is_not_a_declared_member_is_refused(int undeclared) {
        List<CreditSerial> serials = DistinctSerials(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new JournalEntry(1, RecordedAt, (EntryKind)undeclared, Alice, Bob, serials, null));
    }

    [Fact]
    public void An_entry_cannot_debit_and_credit_the_same_account() {
        List<CreditSerial> serials = DistinctSerials(1);

        Assert.Throws<ArgumentException>(() => new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, Alice, serials, null));
    }

    [Fact]
    public void Two_account_objects_with_the_same_id_are_the_same_account() {
        // Arrange
        Account aliceAgain = new Account("FR-000002", FrenchHolder, AccountKind.Holding, RecordedAt.AddYears(-3));
        List<CreditSerial> serials = DistinctSerials(1);

        // Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, aliceAgain, serials, null));
    }

    [Fact]
    public void A_missing_account_or_serial_list_is_refused() {
        List<CreditSerial> serials = DistinctSerials(1);

        Assert.Throws<ArgumentNullException>(() => new JournalEntry(1, RecordedAt, EntryKind.Transfer, null!, Bob, serials, null));
        Assert.Throws<ArgumentNullException>(() => new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, null!, serials, null));
        Assert.Throws<ArgumentNullException>(() => new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, Bob, null!, null));
    }

    [Fact]
    public void An_entry_moves_at_least_one_unit() {
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, Bob, new List<CreditSerial>(), null));
    }

    [Fact]
    public void An_entry_cannot_list_a_null_serial() {
        // Arrange
        List<CreditSerial?> serials = [Any.CreditSerial().Generate(), null];

        // Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, Bob, serials!, null));
    }

    [Fact]
    public void An_entry_cannot_list_the_same_unit_twice_even_as_two_distinct_objects() {
        // Arrange: two instances, one value
        List<CreditSerial> serials = [new CreditSerial("VCS", 1234, 2019, 42), new CreditSerial("VCS", 1234, 2019, 42)];

        // Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, Bob, serials, null));
    }

    [Fact]
    public void Units_that_differ_in_one_part_are_two_units() {
        List<CreditSerial> serials = [new CreditSerial("VCS", 1234, 2019, 42), new CreditSerial("VCS", 1234, 2019, 43)];

        JournalEntry entry = new JournalEntry(1, RecordedAt, EntryKind.Transfer, Alice, Bob, serials, null);

        Assert.Equal(2, entry.Quantity);
    }

    [Fact]
    public void A_retirement_must_state_its_claim() {
        List<CreditSerial> serials = DistinctSerials(1);

        Assert.Throws<ArgumentNullException>(() => new JournalEntry(1, RecordedAt, EntryKind.Retirement, Alice, Offsets, serials, null));
    }

    [Theory]
    [InlineData(EntryKind.Issuance)]
    [InlineData(EntryKind.Transfer)]
    public void Only_a_retirement_carries_a_claim(EntryKind kind) {
        // Arrange
        (AccountKind debitKind, AccountKind creditKind) = AccountKindsOf(kind);
        Account            debit   = Accounts.Of(debitKind);
        Account            credit  = Accounts.OtherThan(debit, creditKind);
        List<CreditSerial> serials = DistinctSerials(1);
        RetirementClaim    anyClaim = LedgerWorld.ClaimCovering(serials);

        // Assert
        Assert.Throws<ArgumentException>(() => new JournalEntry(1, RecordedAt, kind, debit, credit, serials, anyClaim));
    }

    [Fact]
    public void A_claim_may_report_the_very_year_of_the_latest_vintage() {
        // Arrange
        List<CreditSerial> serials = [new CreditSerial("VCS", 1234, 2019, 1), new CreditSerial("VCS", 1234, 2021, 2)];
        RetirementClaim    claim   = new RetirementClaim("Acme Carbon", "fleet", 2021);

        // Act
        JournalEntry entry = new JournalEntry(1, RecordedAt, EntryKind.Retirement, Alice, Offsets, serials, claim);

        // Assert
        Assert.Same(claim, entry.Claim);
    }

    [Fact]
    public void A_claim_may_report_a_year_after_the_latest_vintage() {
        List<CreditSerial> serials = [new CreditSerial("VCS", 1234, 2019, 1), new CreditSerial("VCS", 1234, 2021, 2)];
        RetirementClaim    claim   = new RetirementClaim("Acme Carbon", "fleet", 2022);

        JournalEntry entry = new JournalEntry(1, RecordedAt, EntryKind.Retirement, Alice, Offsets, serials, claim);

        Assert.Equal(2022, entry.Claim!.ReportingYear);
    }

    [Fact]
    public void A_claim_cannot_report_a_year_before_the_latest_vintage_retired() {
        // Arrange: the earlier vintage alone would allow 2020, the later one does not
        List<CreditSerial> serials = [new CreditSerial("VCS", 1234, 2019, 1), new CreditSerial("VCS", 1234, 2021, 2)];
        RetirementClaim    claim   = new RetirementClaim("Acme Carbon", "fleet", 2020);

        // Act
        Action construct = () => new JournalEntry(1, RecordedAt, EntryKind.Retirement, Alice, Offsets, serials, claim);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(construct);
        Assert.Contains("2021", refusal.Message);
        Assert.Contains("2020", refusal.Message);
    }

    [Fact]
    public void The_latest_vintage_counts_whatever_the_order_of_the_serials() {
        List<CreditSerial> serials = [new CreditSerial("VCS", 1234, 2021, 2), new CreditSerial("VCS", 1234, 2019, 1)];
        RetirementClaim    claim   = new RetirementClaim("Acme Carbon", "fleet", 2020);

        Assert.Throws<ArgumentException>(() => new JournalEntry(1, RecordedAt, EntryKind.Retirement, Alice, Offsets, serials, claim));
    }

}
