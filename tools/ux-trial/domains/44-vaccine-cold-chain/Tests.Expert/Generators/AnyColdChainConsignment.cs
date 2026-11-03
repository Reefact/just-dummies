// Started from `any generate ColdChainConsignment`, then rewritten by hand. The scaffolder drew the product, the
// shipment and the pallets independently, and flagged every one: the pallets must carry the product's GTIN, their
// loggers must resolve its single-excursion allowance, the shipment must be a Booked Reefer40, and SSCCs, loggers
// and serials must not repeat. The pallets are therefore drawn AFTER the product, for it.

using System;
using System.Collections.Generic;
using System.Linq;

using ColdChain.Domain;
using JustDummies;
using FreightShipment = Freight.Domain.Shipment;

namespace ColdChain.Tests.Expert.Generators;

public sealed partial class AnyColdChainConsignment : IAny<ColdChainConsignment> {

    private const int MaxAttemptsPerPallet = 100;

    private IAny<ConsignmentId>                                _id             = new AnyConsignmentId();
    private AnyVaccineProduct                                  _product        = new AnyVaccineProduct();
    private VaccineProduct?                                    _productPin;
    private AnyShipment                                        _shipment       = new AnyShipment().WithContainerType(Freight.Domain.ContainerType.Reefer40);
    private FreightShipment?                                   _shipmentPin;
    private IAny<int>                                          _palletCount    = Any.Int32().Between(1, 4);
    private Func<AnyColdChainPallet, AnyColdChainPallet>       _pallet         = pallet => pallet;
    private IReadOnlyList<ColdChainPallet>?                    _pallets;

    public AnyColdChainConsignment() { }

    private AnyColdChainConsignment(AnyColdChainConsignment from) {
        _id          = from._id;
        _product     = from._product;
        _productPin  = from._productPin;
        _shipment    = from._shipment;
        _shipmentPin = from._shipmentPin;
        _palletCount = from._palletCount;
        _pallet      = from._pallet;
        _pallets     = from._pallets;
    }

    public AnyColdChainConsignment WithId(ConsignmentId value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    public AnyColdChainConsignment WithProduct(VaccineProduct value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._productPin = value;

        return copy;
    }

    public AnyColdChainConsignment WithProduct(Func<AnyVaccineProduct, AnyVaccineProduct> configure) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._product    = configure(_product);
        copy._productPin = null;

        return copy;
    }

    public AnyColdChainConsignment WithShipment(FreightShipment value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._shipmentPin = value;

        return copy;
    }

    public AnyColdChainConsignment WithShipment(Func<AnyShipment, AnyShipment> configure) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._shipment    = configure(_shipment);
        copy._shipmentPin = null;

        return copy;
    }

    public AnyColdChainConsignment WithPalletCount(int value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._palletCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines every pallet drawn for the consignment; the product's GTIN and the loggers' interval are applied around it.</summary>
    public AnyColdChainConsignment WithEachPallet(Func<AnyColdChainPallet, AnyColdChainPallet> configure) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);
        Func<AnyColdChainPallet, AnyColdChainPallet> previous = _pallet;

        copy._pallet = pallet => configure(previous(pallet));

        return copy;
    }

    /// <summary>Pins the pallets themselves: nothing about them is drawn or adjusted to the product.</summary>
    public AnyColdChainConsignment WithPallets(IReadOnlyList<ColdChainPallet> value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._pallets = value;

        return copy;
    }

    /// <summary>
    ///     Loggers whose certificates are valid from the day of <paramref name="dispatchedAt" /> to the day of
    ///     <paramref name="deliveredAt" /> (the UTC days the consignment checks them on).
    /// </summary>
    public AnyColdChainConsignment ForTransit(DateTimeOffset dispatchedAt, DateTimeOffset deliveredAt) {
        DateOnly from = DateOnly.FromDateTime(dispatchedAt.UtcDateTime);
        DateOnly to   = DateOnly.FromDateTime(deliveredAt.UtcDateTime);

        return WithEachPallet(pallet => pallet.WithLogger(logger => logger.CoveringDays(from, to)));
    }

    public ColdChainConsignment Generate() {
        VaccineProduct product = _productPin ?? _product.Generate();

        return new ColdChainConsignment(_id.Generate(), product, _shipmentPin ?? _shipment.Generate(), _pallets ?? DrawPalletsFor(product));
    }

    private IReadOnlyList<ColdChainPallet> DrawPalletsFor(VaccineProduct product) {
        int                    allowance = product.Budget.MaxSingleExcursionMinutes;
        List<ColdChainPallet>  pallets   = new List<ColdChainPallet>();
        int                    count     = _palletCount.Generate();
        AnyColdChainPallet     recipe    = new AnyColdChainPallet().WithGtin(product.Gtin);

        if (allowance > 0) {
            // A logger must resolve the product's single-excursion allowance: it records at least that often.
            // This comes BEFORE the caller's own refinements, so one that pins an interval is judged by the domain.
            recipe = recipe.WithLogger(logger => logger.WithRecordingIntervalMinutes(interval => interval.LessThanOrEqualTo(allowance)));
        }

        recipe = _pallet(recipe);

        while (pallets.Count < count) {
            pallets.Add(DrawPalletApartFrom(recipe, pallets));
        }

        return pallets;
    }

    private static ColdChainPallet DrawPalletApartFrom(AnyColdChainPallet recipe, IReadOnlyList<ColdChainPallet> others) {
        for (int attempt = 0; attempt < MaxAttemptsPerPallet; attempt++) {
            ColdChainPallet candidate = recipe.Generate();

            if (!others.Any(other => Collides(candidate, other))) {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not draw a pallet apart from the others in " + MaxAttemptsPerPallet + " attempts.");
    }

    private static bool Collides(ColdChainPallet candidate, ColdChainPallet other) {
        return candidate.Sscc.Equals(other.Sscc)
            || candidate.Logger.Id.Equals(other.Logger.Id)
            || candidate.Packs.Any(pack => other.Carries(pack.Serial));
    }

}
