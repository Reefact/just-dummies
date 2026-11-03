// Started from `any generate ColdChainPallet`, then rewritten by hand. The scaffolder drew `packs` as a list of
// independent packs, but a pallet carries ONE product (one GTIN), with serial numbers that never repeat.
// A pallet here carries 1 to 12 packs: far from the 5 000 the domain allows, which a test pins when it matters.

using System;
using System.Collections.Generic;
using System.Linq;

using AgriTech.Domain;
using ColdChain.Domain;
using JustDummies;
using Serialisation.Domain;
using Warehouse.Domain;

namespace ColdChain.Tests.Expert.Generators;

public sealed partial class AnyColdChainPallet : IAny<ColdChainPallet> {

    private IAny<Sscc>                                    _sscc      = new AnySscc();
    private AnyDataLogger                                 _logger    = new AnyDataLogger();
    private IAny<Gtin>                                    _gtin      = new AnyGtin();
    private IAny<int>                                     _packCount = Any.Int32().Between(1, 12);
    private Func<AnySerialisedPack, AnySerialisedPack>    _pack      = pack => pack.WithExpiresOn(expiry => expiry.Between(new DateOnly(2040, 1, 1), new DateOnly(2060, 12, 31)));
    private IReadOnlyList<SerialisedPack>?                _packs;

    public AnyColdChainPallet() { }

    private AnyColdChainPallet(AnyColdChainPallet from) {
        _sscc      = from._sscc;
        _logger    = from._logger;
        _gtin      = from._gtin;
        _packCount = from._packCount;
        _pack      = from._pack;
        _packs     = from._packs;
    }

    public AnyColdChainPallet WithSscc(Sscc value) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._sscc = Any.Fixed(value);

        return copy;
    }

    public AnyColdChainPallet WithLogger(Func<AnyDataLogger, AnyDataLogger> configure) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._logger = configure(_logger);

        return copy;
    }

    public AnyColdChainPallet WithGtin(Gtin value) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._gtin = Any.Fixed(value);

        return copy;
    }

    public AnyColdChainPallet WithPackCount(int value) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._packCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines every pack drawn for the pallet (its batch, its expiry); the GTIN and the serials stay the pallet's business.</summary>
    public AnyColdChainPallet WithEachPack(Func<AnySerialisedPack, AnySerialisedPack> configure) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);
        Func<AnySerialisedPack, AnySerialisedPack> previous = _pack;

        copy._pack = pack => configure(previous(pack));

        return copy;
    }

    /// <summary>Pins the packs themselves, which then decide the pallet's GTIN.</summary>
    public AnyColdChainPallet WithPacks(IReadOnlyList<SerialisedPack> value) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._packs = value;

        return copy;
    }

    public ColdChainPallet Generate() {
        return new ColdChainPallet(_sscc.Generate(), _logger.Generate(), _packs ?? DrawPacks());
    }

    private IReadOnlyList<SerialisedPack> DrawPacks() {
        Gtin                 gtin    = _gtin.Generate();
        HashSet<SerialNumber> serials = Any.SetOf(new AnySerialNumber()).WithCount(_packCount.Generate()).Generate();

        return serials.Select(serial => _pack(new AnySerialisedPack().WithGtin(gtin).WithSerial(serial)).Generate()).ToList();
    }

}
