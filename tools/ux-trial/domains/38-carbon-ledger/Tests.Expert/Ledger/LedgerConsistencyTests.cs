using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;
using CarbonLedger.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Ledger;

public sealed class LedgerConsistencyTests {

    [Fact]
    public void Balances_of_all_accounts_sum_to_zero_after_an_issuance() {
        RegistryScene scene = RegistryScene.Open();

        scene.IssueTo(scene.First, 3);

        Assert.Equal(0, SumOfBalances(scene));
    }

    [Fact]
    public void Balances_of_all_accounts_sum_to_zero_whatever_the_activity() {
        // Arrange: how many units are issued, moved and retired is arbitrary; the books must balance whatever it is.
        RegistryScene scene     = RegistryScene.Open();
        int           anyIssued = Any.Int32().Between(1, 20).Generate();
        int           anyMoved  = Any.Int32().Between(1, anyIssued).Generate();
        int           anyRetired = Any.Int32().Between(1, anyMoved).Generate();

        // Act
        IReadOnlyList<CreditSerial> issued = scene.IssueTo(scene.First, anyIssued, day: 1);
        scene.Ledger.Transfer(issued.Take(anyMoved).ToList(), scene.First.AccountId, scene.Second.AccountId, scene.At(2));
        scene.RetireFrom(scene.Second, issued.Take(anyRetired).ToList(), 3);

        // Assert
        Assert.Equal(0, SumOfBalances(scene));
        Assert.True(scene.Ledger.IsConsistent());
    }

    [Fact]
    public void A_ledger_stays_consistent_through_a_mix_of_issuances_transfers_and_retirements() {
        // Arrange
        RegistryScene               scene  = RegistryScene.Open();
        IReadOnlyList<CreditSerial> forOne = scene.IssueTo(scene.First, 4, day: 1);
        IReadOnlyList<CreditSerial> forTwo = scene.IssueTo(scene.Second, 3, day: 1);

        // Act
        scene.Ledger.Transfer(forOne.Take(2).ToList(), scene.First.AccountId, scene.Third.AccountId, scene.At(2));
        scene.Ledger.Transfer(forTwo.Take(1).ToList(), scene.Second.AccountId, scene.First.AccountId, scene.At(3));
        scene.RetireFrom(scene.Third, forOne.Take(1).ToList(), 4);
        scene.Ledger.Transfer(forOne.Skip(1).Take(1).ToList(), scene.Third.AccountId, scene.Second.AccountId, scene.At(5));

        // Assert
        Assert.True(scene.Ledger.IsConsistent());
        Assert.Equal(7, scene.Ledger.IssuedQuantity);
        Assert.Equal(1, scene.Ledger.RetiredQuantity);
    }

    [Fact]
    public void An_account_balance_is_the_number_of_units_it_holds() {
        RegistryScene scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 4, day: 1);
        scene.Ledger.Transfer(units.Take(3).ToList(), scene.First.AccountId, scene.Second.AccountId, scene.At(2));

        foreach (Account account in scene.Ledger.Accounts.Where(account => account.Kind != AccountKind.Issuance)) {
            Assert.Equal(scene.Ledger.UnitsHeldBy(account.AccountId).Count, scene.Ledger.Balance(account.AccountId));
        }
    }

    [Fact]
    public void A_ledger_is_consistent_after_operations_were_refused() {
        // Arrange
        RegistryScene               scene = RegistryScene.Open();
        IReadOnlyList<CreditSerial> units = scene.IssueTo(scene.First, 2, day: 1);

        // Act
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Transfer(units, scene.Second.AccountId, scene.Third.AccountId, scene.At(2)));
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.Issue(units, scene.Second.AccountId, scene.At(2)));
        Assert.Throws<ArgumentException>(() => scene.Ledger.Transfer(units, scene.First.AccountId, scene.First.AccountId, scene.At(2)));

        // Assert
        Assert.True(scene.Ledger.IsConsistent());
        Assert.Equal(2, scene.Ledger.Balance(scene.First.AccountId));
    }

    private static int SumOfBalances(RegistryScene scene) {
        return scene.Ledger.Accounts.Sum(account => scene.Ledger.Balance(account.AccountId));
    }

}
