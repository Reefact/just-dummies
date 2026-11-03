using System.Numerics;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class LedgerInvariantTests {

    private static BigInteger SumOfBalances(Ledger ledger) {
        return ledger.Accounts.Aggregate(BigInteger.Zero, (sum, account) => sum + account.Balance.BaseUnits);
    }

    // One of every operation, between the same five addresses.
    private static Chain AfterEveryKindOfTransaction() {
        Chain chain = Chain.Start();
        chain.Fund(chain.Minter, Chain.Funds).FundAlice().FundBob();

        chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Carol, Amount.Of(700));
        chain.Ledger.Approve(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(1_000));
        chain.Ledger.IncreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(50));
        chain.Ledger.DecreaseAllowance(chain.Envelope(chain.Alice).Generate(), chain.Bob, Amount.Of(20));
        chain.Ledger.TransferFrom(chain.Envelope(chain.Bob).Generate(), chain.Alice, chain.Carol, Amount.Of(300));
        chain.Ledger.Mint(chain.Envelope(chain.Minter).Generate(), chain.Bob, Amount.Of(5_000));
        chain.Ledger.Burn(chain.Envelope(chain.Bob).Generate(), Amount.Of(1_200));

        return chain;
    }

    [Fact]
    public void The_balances_always_add_up_to_the_supply() {
        // Arrange
        Chain chain = AfterEveryKindOfTransaction();

        // Assert
        Assert.Equal(chain.Ledger.TotalSupply.BaseUnits, SumOfBalances(chain.Ledger));
    }

    [Fact]
    public void The_supply_is_what_was_minted_less_what_was_burnt_and_fees_change_nothing_of_it() {
        // Arrange: Funds to the minter, Alice and Bob; 5_000 minted; 1_200 burnt.
        Chain chain = AfterEveryKindOfTransaction();

        // Assert
        Assert.Equal(3 * Chain.Funds + 5_000 - 1_200, (long)chain.Ledger.TotalSupply.BaseUnits);
    }

    [Fact]
    public void The_collector_holds_exactly_the_fees_of_the_accepted_transactions() {
        // Arrange
        Chain chain = AfterEveryKindOfTransaction();

        // Act
        BigInteger fees = chain.Ledger.Receipts.Aggregate(BigInteger.Zero, (sum, receipt) => sum + receipt.Fee.BaseUnits);

        // Assert: the three fundings were free; the seven transactions paid 10 per gas.
        Assert.Equal(fees, chain.BalanceOf(chain.Collector));
        Assert.True(fees > 0);
    }

    [Fact]
    public void Every_accepted_transaction_leaves_a_receipt_and_the_senders_nonces_count_them() {
        // Arrange
        Chain chain = AfterEveryKindOfTransaction();

        // Assert: Minter 3 fundings + 1 mint, Alice 4, Bob 2, Carol 0.
        Assert.Equal(3 + 7, chain.Ledger.Receipts.Count);
        Assert.Equal((UInt128)4, chain.Ledger.NextNonceOf(chain.Minter));
        Assert.Equal((UInt128)4, chain.Ledger.NextNonceOf(chain.Alice));
        Assert.Equal((UInt128)2, chain.Ledger.NextNonceOf(chain.Bob));
        Assert.Equal((UInt128)0, chain.Ledger.NextNonceOf(chain.Carol));
    }

    [Fact]
    public void The_receipts_follow_the_order_of_the_transactions() {
        // Arrange
        Chain chain = AfterEveryKindOfTransaction();

        // Act
        IEnumerable<TransactionKind> kinds = chain.Ledger.Receipts.Skip(3).Select(receipt => receipt.Kind);

        // Assert
        Assert.Equal(
            [
                TransactionKind.Transfer, TransactionKind.Approve, TransactionKind.IncreaseAllowance, TransactionKind.DecreaseAllowance,
                TransactionKind.TransferFrom, TransactionKind.Mint, TransactionKind.Burn,
            ],
            kinds);
    }

    [Fact]
    public void An_address_becomes_an_account_when_it_is_credited() {
        // Arrange
        Chain chain = Chain.Start().FundAlice().FundBob();

        // Act
        chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Carol, Amount.Of(1));

        // Assert
        Account carol = Assert.Single(chain.Ledger.Accounts, account => account.Address.Equals(chain.Carol));
        Assert.True(carol.IsHolder);
        Assert.Equal(Amount.Of(1), carol.Balance);
    }

    [Fact]
    public void A_refused_transfer_creates_no_account_for_its_recipient() {
        // Arrange
        Chain chain = Chain.Start().FundAlice(100).FundBob();

        // Act
        chain.Refuses(RefusalReason.InsufficientBalance,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).Generate(), chain.Carol, Amount.Of(101)));

        // Assert
        Assert.DoesNotContain(chain.Ledger.Accounts, account => account.Address.Equals(chain.Carol));
    }

    [Fact]
    public void A_refused_transaction_leaves_the_ledger_as_it_found_it_whatever_the_reason() {
        // Arrange: three different refusals in a row, each a different reason, none leaving a mark.
        Chain chain = Chain.Start().FundAlice(1_500_000).FundBob();

        // Act / Assert
        chain.Refuses(RefusalReason.NonceTooHigh,
                      () => chain.Ledger.Transfer(chain.Envelope(chain.Alice).WithNonce(5).Generate(), chain.Bob, Amount.Of(1)));
        chain.Refuses(RefusalReason.InsufficientBalance,
                      () => chain.Ledger.Burn(chain.Envelope(chain.Alice).Generate(), Amount.Of(1_500_001)));
        chain.Refuses(RefusalReason.AllowanceExceeded,
                      () => chain.Ledger.TransferFrom(chain.Envelope(chain.Alice).Generate(), chain.Bob, chain.Carol, Amount.Of(1)));
    }

}
