using ColdChain.Domain;

using JustDummies;

using Serialisation.Domain;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class ColdChainPalletTests {

    private static SerialisedPack PackOf(Gtin gtin) {
        return new AnySerialisedPack().WithGtin(gtin).Generate();
    }

    private static IReadOnlyList<SerialisedPack> PacksOf(Gtin gtin, int count) {
        return Any.ListOf(new AnySerialNumber()).WithCount(count).Distinct().Generate()
                  .Select(serial => new AnySerialisedPack().WithGtin(gtin).WithSerial(serial).Generate())
                  .ToList();
    }

    private static SerialisedPack RecalledPackOf(Gtin gtin) {
        Batch batch = new Batch(gtin, new AnyBatchNumber().Generate(), new DateOnly(2025, 1, 1), new DateOnly(2030, 1, 1), 10, new ReimbursementPrice(12.34m));
        SerialisedPack pack = batch.Commission(new AnySerialNumber().Generate());
        batch.Recall();

        return pack;
    }

    [Fact]
    public void A_pallet_keeps_its_label_its_logger_and_its_packs() {
        // Arrange
        Sscc                          anySscc   = new AnySscc().Generate();
        DataLogger                    anyLogger = new AnyDataLogger().Generate();
        Gtin                          anyGtin   = new AnyGtin().Generate();
        IReadOnlyList<SerialisedPack> packs     = PacksOf(anyGtin, 12);

        // Act
        ColdChainPallet pallet = new ColdChainPallet(anySscc, anyLogger, packs);

        // Assert
        Assert.Same(anySscc, pallet.Sscc);
        Assert.Same(anyLogger, pallet.Logger);
        Assert.Equal(packs, pallet.Packs);
        Assert.Equal(12, pallet.PackCount);
        Assert.Equal(anyGtin, pallet.Gtin);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(ColdChainPallet.MaxPacks)]
    public void A_pallet_carries_from_one_to_five_thousand_packs(int count) {
        // Arrange
        Gtin anyGtin = new AnyGtin().Generate();

        // Act
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(PacksOf(anyGtin, count)).Generate();

        // Assert
        Assert.Equal(count, pallet.PackCount);
    }

    [Fact]
    public void A_pallet_without_any_pack_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new AnyColdChainPallet().WithPacks([]).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("packs", refusal.ParamName);
    }

    [Fact]
    public void A_pallet_beyond_five_thousand_packs_is_refused() {
        // Arrange
        Gtin anyGtin = new AnyGtin().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new AnyColdChainPallet().WithPacks(PacksOf(anyGtin, ColdChainPallet.MaxPacks + 1)).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("packs", refusal.ParamName);
    }

    [Fact]
    public void A_missing_part_is_refused() {
        // Arrange
        Sscc                          anySscc   = new AnySscc().Generate();
        DataLogger                    anyLogger = new AnyDataLogger().Generate();
        IReadOnlyList<SerialisedPack> anyPacks  = PacksOf(new AnyGtin().Generate(), 2);

        // Assert
        Assert.Throws<ArgumentNullException>(() => new ColdChainPallet(null!, anyLogger, anyPacks));
        Assert.Throws<ArgumentNullException>(() => new ColdChainPallet(anySscc, null!, anyPacks));
        Assert.Throws<ArgumentNullException>(() => new ColdChainPallet(anySscc, anyLogger, null!));
    }

    [Fact]
    public void A_pallet_cannot_list_a_missing_pack() {
        // Arrange
        Gtin anyGtin = new AnyGtin().Generate();
        List<SerialisedPack> packs = [PackOf(anyGtin), null!, PackOf(anyGtin)];

        // Act
        Exception? thrown = Record.Exception(() => new AnyColdChainPallet().WithPacks(packs).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("packs", refusal.ParamName);
    }

    [Fact]
    public void A_serial_number_cannot_appear_twice() {
        // Arrange
        Gtin         anyGtin   = new AnyGtin().Generate();
        SerialNumber anySerial = new AnySerialNumber().Generate();
        List<SerialisedPack> packs = [
            new AnySerialisedPack().WithGtin(anyGtin).WithSerial(anySerial).Generate(),
            PackOf(anyGtin),
            new AnySerialisedPack().WithGtin(anyGtin).WithSerial(anySerial).Generate()
        ];

        // Act
        Exception? thrown = Record.Exception(() => new AnyColdChainPallet().WithPacks(packs).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("packs", refusal.ParamName);
    }

    [Fact]
    public void A_pack_that_has_left_the_supply_chain_is_refused() {
        // Arrange
        Gtin anyGtin = new AnyGtin().Generate();
        List<SerialisedPack> packs = [PackOf(anyGtin), RecalledPackOf(anyGtin)];

        // Act
        Exception? thrown = Record.Exception(() => new AnyColdChainPallet().WithPacks(packs).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("packs", refusal.ParamName);
    }

    [Fact]
    public void A_pallet_carries_a_single_product() {
        // Arrange
        Gtin gtin        = new AnyGtin().Generate();
        Gtin anotherGtin = new AnyGtin().DifferentFrom(gtin).Generate();
        List<SerialisedPack> packs = [PackOf(gtin), PackOf(gtin), PackOf(anotherGtin)];

        // Act
        Exception? thrown = Record.Exception(() => new AnyColdChainPallet().WithPacks(packs).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("packs", refusal.ParamName);
    }

    [Fact]
    public void Several_batches_of_the_product_may_share_a_pallet() {
        // Arrange
        Gtin        anyGtin     = new AnyGtin().Generate();
        BatchNumber firstBatch  = new BatchNumber("LOT-A1");
        BatchNumber secondBatch = new BatchNumber("LOT-B2");
        List<SerialisedPack> packs = [
            new AnySerialisedPack().WithGtin(anyGtin).WithBatch(firstBatch).Generate(),
            new AnySerialisedPack().WithGtin(anyGtin).WithBatch(secondBatch).Generate(),
            new AnySerialisedPack().WithGtin(anyGtin).WithBatch(firstBatch).Generate()
        ];

        // Act
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(packs).Generate();

        // Assert
        Assert.Equal(2, pallet.Batches.Count);
        Assert.Contains(firstBatch, pallet.Batches);
        Assert.Contains(secondBatch, pallet.Batches);
    }

    [Fact]
    public void The_earliest_expiry_is_that_of_the_pack_expiring_first() {
        // Arrange
        Gtin anyGtin = new AnyGtin().Generate();
        List<SerialisedPack> packs = [
            new AnySerialisedPack().WithGtin(anyGtin).WithExpiresOn(new DateOnly(2029, 6, 30)).Generate(),
            new AnySerialisedPack().WithGtin(anyGtin).WithExpiresOn(new DateOnly(2028, 2, 29)).Generate(),
            new AnySerialisedPack().WithGtin(anyGtin).WithExpiresOn(new DateOnly(2031, 1, 1)).Generate()
        ];

        // Act
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(packs).Generate();

        // Assert
        Assert.Equal(new DateOnly(2028, 2, 29), pallet.EarliestExpiry);
    }

    [Fact]
    public void A_pallet_knows_which_serials_it_carries() {
        // Arrange
        Gtin         anyGtin       = new AnyGtin().Generate();
        SerialNumber carried       = new AnySerialNumber().Generate();
        SerialNumber notCarried    = new AnySerialNumber().DifferentFrom(carried).Generate();
        ColdChainPallet pallet     = new AnyColdChainPallet().WithPacks([PackOf(anyGtin), new AnySerialisedPack().WithGtin(anyGtin).WithSerial(carried).Generate()]).Generate();

        // Assert
        Assert.True(pallet.Carries(carried));
        Assert.False(pallet.Carries(notCarried));
    }

    [Fact]
    public void The_pack_list_is_a_copy_the_caller_cannot_change_afterwards() {
        // Arrange
        Gtin                 anyGtin = new AnyGtin().Generate();
        List<SerialisedPack> packs   = [PackOf(anyGtin), PackOf(anyGtin)];
        ColdChainPallet      pallet  = new AnyColdChainPallet().WithPacks(packs).Generate();

        // Act
        packs.Add(PackOf(anyGtin));

        // Assert
        Assert.Equal(2, pallet.PackCount);
    }

    [Fact]
    public void A_pallet_reads_back_its_label_and_its_pack_count() {
        // Arrange
        Sscc            anySscc = new AnySscc().Generate();
        ColdChainPallet pallet  = new AnyColdChainPallet().WithSscc(anySscc).WithPacks(PacksOf(new AnyGtin().Generate(), 7)).Generate();

        // Assert
        Assert.Equal($"{anySscc} (7 packs)", pallet.ToString());
    }

    [Fact]
    public void The_generator_only_draws_pallets_the_constructor_accepts() {
        // Act
        ColdChainPallet anyPallet = new AnyColdChainPallet().Generate();

        // Assert
        Assert.InRange(anyPallet.PackCount, 1, ColdChainPallet.MaxPacks);
        Assert.All(anyPallet.Packs, pack => Assert.Equal(anyPallet.Gtin, pack.Gtin));
    }

}
