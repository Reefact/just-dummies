// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ColdChainPallet --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;
using Serialisation.Domain;
using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ColdChainPallet" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyColdChainPallet : IAny<ColdChainPallet> {

    private IAny<Sscc>                          _sscc;
    private IAny<DataLogger>                    _logger;
    private IAny<IReadOnlyList<SerialisedPack>> _packs;

    private AnySscc                 _ssccRecipe;
    private AnyDataLogger           _loggerRecipe;
    private IAny<IReadOnlyList<SerialisedPack>> _packsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyColdChainPallet()
        : this(sscc:   AnyValidSscc(),
               logger: AnyValidLogger(),
               packs:  AnyValidPacks()) { }

    private static AnySscc AnyValidSscc() {
        return new AnySscc();
    }

    private static AnyDataLogger AnyValidLogger() {
        return new AnyDataLogger();
    }

    private static IAny<IReadOnlyList<SerialisedPack>> AnyValidPacks() {
        // One product per pallet (one GTIN), distinct serials, batches that may mix. A few dozen packs stand for
        // a pallet: the 5 000 ceiling is a boundary to pin in a test of its own, not a size to draw.
        return new AnyGtin().Then(gtin => PacksOf(gtin));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyColdChainPallet(AnySscc                 sscc,
                               AnyDataLogger           logger,
                               IAny<IReadOnlyList<SerialisedPack>> packs) {
        _sscc         = sscc;
        _ssccRecipe   = sscc;
        _logger       = logger;
        _loggerRecipe = logger;
        _packs        = packs;
        _packsRecipe  = packs;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyColdChainPallet(AnyColdChainPallet from) {
        _sscc         = from._sscc;
        _ssccRecipe   = from._ssccRecipe;
        _logger       = from._logger;
        _loggerRecipe = from._loggerRecipe;
        _packs        = from._packs;
        _packsRecipe  = from._packsRecipe;
    }

    /// <summary>Pins <c>sscc</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyColdChainPallet WithSscc(Sscc value) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._sscc = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sscc</c> is drawn from — <c>AnyValidSscc()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySscc, AnySscc&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyColdChainPallet WithSscc(Func<AnySscc, AnySscc> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._ssccRecipe = configure(_ssccRecipe);
        copy._sscc       = copy._ssccRecipe;

        return copy;
    }

    /// <summary>Pins <c>logger</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyColdChainPallet WithLogger(DataLogger value) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._logger = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>logger</c> is drawn from — <c>AnyValidLogger()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDataLogger, AnyDataLogger&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyColdChainPallet WithLogger(Func<AnyDataLogger, AnyDataLogger> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._loggerRecipe = configure(_loggerRecipe);
        copy._logger       = copy._loggerRecipe;

        return copy;
    }

    /// <summary>Pins <c>packs</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyColdChainPallet WithPacks(IReadOnlyList<SerialisedPack> value) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._packs = Any.Fixed(value);

        return copy;
    }


    /// <summary>
    ///     Loads the pallet for <paramref name="product" />: its GTIN on every pack, and a logger that records often enough
    ///     to resolve the product's single-excursion allowance, which a consignment requires.
    /// </summary>
    public AnyColdChainPallet ForProduct(VaccineProduct product) {
        int allowance = product.Budget.MaxSingleExcursionMinutes;
        int slowest   = allowance > 0 ? Math.Min(60, allowance) : 60;

        return OfProduct(product.Gtin).WithLogger(logger => logger.WithRecordingIntervalMinutes(interval => interval.Between(1, slowest)));
    }

    /// <summary>Loads the pallet with packs of the product <paramref name="gtin" />: every pack of a pallet carries one GTIN.</summary>
    public AnyColdChainPallet OfProduct(Gtin gtin) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._packs = PacksOf(gtin);

        return copy;
    }

    /// <summary>Packs of the product <paramref name="gtin" />, with distinct serials and one to three batches among them.</summary>
    public static IAny<IReadOnlyList<SerialisedPack>> PacksOf(Gtin gtin) {
        return Any.ListOf(new AnySerialNumber()).WithCountBetween(1, 30).Distinct()
                  .Then(serials => Any.Combine(
                      Any.ListOf(new AnyBatchNumber()).WithCountBetween(1, 3).Distinct(),
                      Any.ListOf(AnySerialisedPack.ValidExpiresOn()).WithCount(serials.Count),
                      (batches, expiries) => (IReadOnlyList<SerialisedPack>)serials
                          .Select((serial, index) => new SerialisedPack(gtin, batches[index % batches.Count], expiries[index], serial))
                          .ToList()));
    }

    /// <summary>Produces one arbitrary <see cref="ColdChainPallet" />.</summary>
    public ColdChainPallet Generate() {
        return new ColdChainPallet(_sscc.Generate(),
                                   _logger.Generate(),
                                   _packs.Generate());
    }

}
