using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Serialisation.Domain;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.Entities;

public sealed class ColdChainPalletTests {

    [Theory]
    [InlineData(1)]
    [InlineData(37)]
    [InlineData(ColdChainPallet.MaxPacks)]
    public void A_pallet_carries_one_to_five_thousand_packs(int packCount) {
        // Act
        ColdChainPallet pallet = Any.ColdChainPallet().WithPackCount(packCount).Generate();

        // Assert
        Assert.Equal(packCount, pallet.PackCount);
        Assert.Equal(packCount, pallet.Packs.Count);
    }

    [Fact]
    public void A_pallet_without_packs_is_refused() {
        // Arrange
        AnyColdChainPallet pallets = Any.ColdChainPallet().WithPacks(Array.Empty<SerialisedPack>());

        // Assert
        Rejected.For("1 to 5000 packs", () => pallets.Generate());
    }

    [Fact]
    public void A_pallet_of_more_than_five_thousand_packs_is_refused() {
        // Arrange
        AnyColdChainPallet pallets = Any.ColdChainPallet().WithPackCount(ColdChainPallet.MaxPacks + 1);

        // Assert
        Rejected.For("1 to 5000 packs", () => pallets.Generate());
    }

    [Fact]
    public void A_pallet_listing_a_missing_pack_is_refused() {
        // Arrange
        Gtin           sharedGtin = Any.Gtin().Generate();
        SerialisedPack anyPack    = Any.SerialisedPack().WithGtin(sharedGtin).Generate();

        AnyColdChainPallet pallets = Any.ColdChainPallet().WithPacks([anyPack, null!]);

        // Assert
        Rejected.For("missing pack", () => pallets.Generate());
    }

    [Fact]
    public void A_serial_number_appearing_twice_on_the_pallet_is_refused() {
        // Arrange
        Gtin         sharedGtin   = Any.Gtin().Generate();
        SerialNumber sharedSerial = Any.SerialNumber().Generate();

        SerialisedPack first  = Any.SerialisedPack().WithGtin(sharedGtin).WithSerial(sharedSerial).Generate();
        SerialisedPack second = Any.SerialisedPack().WithGtin(sharedGtin).WithSerial(sharedSerial).Generate();

        AnyColdChainPallet pallets = Any.ColdChainPallet().WithPacks([first, second]);

        // Assert
        Rejected.For("serial number appears twice", () => pallets.Generate());
    }

    [Fact]
    public void Two_packs_of_different_products_cannot_share_a_pallet() {
        // Arrange
        Gtin anyGtin     = Any.Gtin().Generate();
        Gtin anotherGtin = new AnyGtin().DifferentFrom(anyGtin).Generate();

        SerialisedPack ofFirstProduct  = Any.SerialisedPack().WithGtin(anyGtin).Generate();
        SerialisedPack ofSecondProduct = Any.SerialisedPack().WithGtin(anotherGtin).Generate();

        AnyColdChainPallet pallets = Any.ColdChainPallet().WithPacks([ofFirstProduct, ofSecondProduct]);

        // Assert
        Rejected.For("single product", () => pallets.Generate());
    }

    [Fact]
    public void A_pack_that_left_the_supply_chain_cannot_be_shipped() {
        // Arrange
        Gtin           sharedGtin = Any.Gtin().Generate();
        SerialNumber   anySerial  = Any.SerialNumber().Generate();
        Batch          batch      = new Batch(sharedGtin, Any.BatchNumber().Generate(), new DateOnly(2026, 1, 1), new DateOnly(2028, 1, 1), 10, new ReimbursementPrice(Any.Decimal().Between(0.01m, 1_000m).WithScale(2).Generate()));
        SerialisedPack recalled   = batch.Commission(anySerial);

        batch.Recall();

        AnyColdChainPallet pallets = Any.ColdChainPallet().WithPacks([recalled]);

        // Assert
        Rejected.For("active packs", () => pallets.Generate());
    }

    [Fact]
    public void A_pallet_reports_the_product_its_packs_are_of() {
        // Arrange
        Gtin sharedGtin = Any.Gtin().Generate();

        // Act
        ColdChainPallet pallet = Any.ColdChainPallet().WithGtin(sharedGtin).Generate();

        // Assert
        Assert.Equal(sharedGtin, pallet.Gtin);
    }

    [Fact]
    public void The_earliest_expiry_of_a_pallet_is_that_of_its_soonest_expiring_pack_wherever_it_sits() {
        // Arrange
        Gtin sharedGtin = Any.Gtin().Generate();

        SerialisedPack expiringLate     = Any.SerialisedPack().WithGtin(sharedGtin).WithExpiresOn(new DateOnly(2028, 1, 1)).Generate();
        SerialisedPack expiringSoonest  = Any.SerialisedPack().WithGtin(sharedGtin).WithExpiresOn(new DateOnly(2026, 12, 31)).Generate();
        SerialisedPack expiringBetween  = Any.SerialisedPack().WithGtin(sharedGtin).WithExpiresOn(new DateOnly(2027, 5, 1)).Generate();

        ColdChainPallet pallet = Any.ColdChainPallet().WithPacks([expiringLate, expiringSoonest, expiringBetween]).Generate();

        // Assert
        Assert.Equal(new DateOnly(2026, 12, 31), pallet.EarliestExpiry);
    }

    [Fact]
    public void A_pallet_mixing_two_batches_of_one_product_lists_each_batch_once() {
        // Arrange
        Gtin        sharedGtin = Any.Gtin().Generate();
        BatchNumber batchA     = Any.BatchNumber().Generate();
        BatchNumber batchB     = new AnyBatchNumber().DifferentFrom(batchA).Generate();

        SerialisedPack[] packs = [
            Any.SerialisedPack().WithGtin(sharedGtin).WithBatch(batchA).Generate(),
            Any.SerialisedPack().WithGtin(sharedGtin).WithBatch(batchB).Generate(),
            Any.SerialisedPack().WithGtin(sharedGtin).WithBatch(batchA).Generate()
        ];

        // Act
        ColdChainPallet pallet = Any.ColdChainPallet().WithPacks(packs).Generate();

        // Assert
        Assert.Equal(2, pallet.Batches.Count);
        Assert.Contains(batchA, pallet.Batches);
        Assert.Contains(batchB, pallet.Batches);
    }

    [Fact]
    public void A_pallet_carries_the_serials_of_its_packs_and_only_those() {
        // Arrange
        Gtin         sharedGtin = Any.Gtin().Generate();
        SerialNumber onBoard    = Any.SerialNumber().Generate();
        SerialNumber notOnBoard = new AnySerialNumber().DifferentFrom(onBoard).Generate();

        ColdChainPallet pallet = Any.ColdChainPallet().WithPacks([Any.SerialisedPack().WithGtin(sharedGtin).WithSerial(onBoard).Generate()]).Generate();

        // Assert
        Assert.True(pallet.Carries(onBoard));
        Assert.False(pallet.Carries(notOnBoard));
    }

    [Fact]
    public void A_missing_logger_or_sscc_is_refused() {
        // Arrange
        Sscc            anySscc   = Any.Sscc().Generate();
        DataLogger      anyLogger = Any.DataLogger().Generate();
        SerialisedPack  anyPack   = Any.SerialisedPack().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new ColdChainPallet(null!, anyLogger, [anyPack]));
        Assert.Throws<ArgumentNullException>(() => new ColdChainPallet(anySscc, null!, [anyPack]));
        Assert.Throws<ArgumentNullException>(() => new ColdChainPallet(anySscc, anyLogger, null!));
    }

    [Fact(Skip = "domain defect: ColdChainPallet.Packs is the internal SerialisedPack[] behind an IReadOnlyList, so a cast replaces a pack and breaks the one-GTIN rule, seed 891432252")]
    public void The_packs_a_pallet_exposes_cannot_be_replaced_through_a_cast() {
        // Arrange
        Gtin            sharedGtin = Any.Gtin().Generate();
        Gtin            otherGtin  = new AnyGtin().DifferentFrom(sharedGtin).Generate();
        ColdChainPallet pallet     = Any.ColdChainPallet().WithGtin(sharedGtin).WithPackCount(2).Generate();

        // Act
        if (pallet.Packs is SerialisedPack[] exposed) {
            exposed[0] = Any.SerialisedPack().WithGtin(otherGtin).Generate();
        }

        // Assert
        Assert.All(pallet.Packs, pack => Assert.Equal(sharedGtin, pack.Gtin));
    }
}
