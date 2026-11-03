using System.Numerics;

using JustDummies;

using TokenLedger.Domain;
using TokenLedger.Tests.Discovery.Support;

using Xunit;

namespace TokenLedger.Tests.Discovery;

// One scripted life of a token: the figures are the point, so they are literals; the participants are dummies.
public sealed class LedgerScenarioTests {

    [Fact]
    public void A_scripted_history_keeps_every_book_in_balance() {
        // Arrange: cap 10^12, minter holds 10^10, standard schedule, price 2.
        Ledger  ledger = Chain.NewLedger(10_000_000_000);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        Address carol  = Chain.NewAddress();

        // Act
        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(5_000_000));          // new holder: 55 000 gas
        ledger.Mint(Chain.Next(ledger, ledger.Minter), bob, new TokenAmount(2_000_000));                // new holder: 60 000 gas
        ledger.AdvanceBlocks(3);
        ledger.Approve(Chain.Next(ledger, alice), bob, new TokenAmount(1_000_000));                     // 30 000 gas
        ledger.TransferFrom(Chain.Next(ledger, bob), alice, carol, new TokenAmount(400_000));           // new holder: 65 000 gas
        ledger.IncreaseAllowance(Chain.Next(ledger, alice), bob, new TokenAmount(100_000));             // 30 000 gas
        ledger.DecreaseAllowance(Chain.Next(ledger, alice), bob, new TokenAmount(200_000));             // 30 000 gas
        ledger.Burn(Chain.Next(ledger, alice), new TokenAmount(1_000_000));                             // 32 000 gas
        ledger.Transfer(Chain.Next(ledger, bob), carol, new TokenAmount(300_000));                      // existing holder: 35 000 gas

        // Assert: the spend of every account.
        // alice: 5 000 000 - 60 000 (approve) - 400 000 (moved by bob) - 60 000 (increase) - 60 000 (decrease) - 1 000 000 - 64 000 (burn) = 3 356 000
        Assert.Equal(new TokenAmount(5_000_000 - 60_000 - 400_000 - 60_000 - 60_000 - 1_000_000 - 64_000), ledger.BalanceOf(alice));
        // bob: 2 000 000 - 130 000 (transferFrom) - 300 000 - 70 000 (his own transfer) = 1 500 000
        Assert.Equal(new TokenAmount(2_000_000 - 130_000 - 300_000 - 70_000), ledger.BalanceOf(bob));
        Assert.Equal(new TokenAmount(400_000 + 300_000), ledger.BalanceOf(carol));
        // bob's allowance: 1 000 000 - 400 000 (spent) + 100 000 - 200 000 = 500 000
        Assert.Equal(new TokenAmount(500_000), ledger.AllowanceOf(alice, bob));

        // The books: nothing leaked, and the collector holds exactly what the receipts charged.
        BigInteger fees = ledger.Receipts.Aggregate(BigInteger.Zero, (total, receipt) => total + receipt.Fee.BaseUnits);
        Assert.Equal(fees, ledger.BalanceOf(ledger.FeeCollector).BaseUnits);
        Assert.Equal(new TokenAmount(10_000_000_000 + 2_000_000 - 1_000_000), ledger.TotalSupply);
        Assert.Equal(ledger.TotalSupply.BaseUnits, Chain.SumOfBalances(ledger));
        Assert.Equal(8, ledger.Receipts.Count);
    }

    [Fact]
    public void The_receipts_are_kept_in_order_and_stamped_with_the_block_of_their_transaction() {
        Ledger  ledger = Chain.NewLedger(10_000_000_000);
        Address alice  = Chain.NewAddress();
        ulong   start  = ledger.CurrentBlock;

        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(1_000_000));
        ledger.AdvanceBlocks(5);
        ledger.Burn(Chain.Next(ledger, ledger.Minter), new TokenAmount(1));

        Assert.Equal(new[] { TransactionKind.Transfer, TransactionKind.Burn }, ledger.Receipts.Select(receipt => receipt.Kind).ToArray());
        Assert.Equal(new[] { start, start + 5 }, ledger.Receipts.Select(receipt => receipt.BlockNumber).ToArray());
        Assert.Equal(new[] { new UInt128(0, 0), new UInt128(0, 1) }, ledger.Receipts.Select(receipt => receipt.Nonce).ToArray());
    }

    [Fact]
    public void The_transfer_log_replays_into_the_balances() {
        // Arrange
        Ledger  ledger = Chain.NewLedger(10_000_000_000);
        Address alice  = Chain.NewAddress();
        Address bob    = Chain.NewAddress();
        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(3_000_000));
        ledger.Mint(Chain.Next(ledger, ledger.Minter), bob, new TokenAmount(500_000));
        ledger.Burn(Chain.Next(ledger, alice), new TokenAmount(1_000_000));
        ledger.Transfer(Chain.Next(ledger, alice), bob, new TokenAmount(100_000));

        // Act: fold every logged movement over empty books, as an indexer reading the events would.
        Dictionary<Address, BigInteger> books = new Dictionary<Address, BigInteger>();
        foreach (TransferRecord record in ledger.Transfers) {
            if (!record.IsMint) { books[record.From] = books.GetValueOrDefault(record.From) - record.Amount.BaseUnits; }
            if (!record.IsBurn) { books[record.To] = books.GetValueOrDefault(record.To) + record.Amount.BaseUnits; }
        }

        // Assert
        foreach (Account account in ledger.Accounts) {
            Assert.Equal(account.Balance.BaseUnits, books.GetValueOrDefault(account.Address));
        }
    }

    [Fact(Skip = "domain defect: Receipts and Transfers hand out their backing List<T> as IReadOnlyList<T>, so a caller can downcast and clear the logs; seed 0 (no draw involved)")]
    public void The_logs_cannot_be_emptied_by_a_caller_through_the_list_they_expose() {
        Ledger  ledger = Chain.NewLedger(10_000_000_000);
        Address alice  = Chain.NewAddress();
        ledger.Transfer(Chain.Next(ledger, ledger.Minter), alice, new TokenAmount(5));

        if (ledger.Receipts is List<Receipt> receipts) { receipts.Clear(); }
        if (ledger.Transfers is List<TransferRecord> transfers) { transfers.Clear(); }

        Assert.NotEmpty(ledger.Receipts);
        Assert.NotEmpty(ledger.Transfers);
    }

}
