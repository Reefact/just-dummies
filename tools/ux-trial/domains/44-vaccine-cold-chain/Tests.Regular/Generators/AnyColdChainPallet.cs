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
    private AnyList<SerialisedPack> _packsRecipe;

    private Gtin? _gtinPin;

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

    private static AnyList<SerialisedPack> AnyValidPacks() {
        // 1 to 5 000 packs in the domain; a dummy pallet stays small. Serials differ, every pack is active (a new pack is),
        // and the single GTIN is settled in Generate().
        return Any.ListOf(new AnySerialisedPack()).WithCountBetween(1, 40).DistinctBy(pack => pack.Serial.Value);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyColdChainPallet(AnySscc                 sscc,
                               AnyDataLogger           logger,
                               AnyList<SerialisedPack> packs) {
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
        _gtinPin      = from._gtinPin;
    }

    /// <summary>Makes every pack of the pallet carry <paramref name="gtin" />: a pallet holds one product.</summary>
    public AnyColdChainPallet WithGtin(Gtin gtin) {
        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._gtinPin = gtin;

        return copy;
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

    /// <summary>Refines the recipe <c>packs</c> is drawn from — <c>AnyValidPacks()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;SerialisedPack&gt;, AnyList&lt;SerialisedPack&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyColdChainPallet WithPacks(Func<AnyList<SerialisedPack>, AnyList<SerialisedPack>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyColdChainPallet copy = new AnyColdChainPallet(this);

        copy._packsRecipe = configure(_packsRecipe);
        copy._packs       = copy._packsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ColdChainPallet" />.</summary>
    public ColdChainPallet Generate() {
        IReadOnlyList<SerialisedPack> packs = _packs.Generate();

        // A pallet carries a single product: the packs drawn alone take the GTIN of the first, or the one asked for.
        if (packs.Count > 0 && (_gtinPin is not null || ReferenceEquals(_packs, _packsRecipe))) {
            Gtin gtin = _gtinPin ?? packs[0].Gtin;
            packs = packs.Select(pack => new SerialisedPack(gtin, pack.Batch, pack.ExpiresOn, pack.Serial)).ToList();
        }

        return new ColdChainPallet(_sscc.Generate(),
                                   _logger.Generate(),
                                   packs);
    }

}
