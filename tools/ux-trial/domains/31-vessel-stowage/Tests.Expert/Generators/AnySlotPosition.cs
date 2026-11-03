// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SlotPosition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Linq;

using JustDummies;
using VesselStowage.Domain;

namespace VesselStowage.Domain.Tests.Generators;

/// <summary>
///     A generator of arbitrary <see cref="SlotPosition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySlotPosition : IAny<SlotPosition> {

    private IAny<int> _bay;
    private IAny<int> _row;
    private IAny<int> _tier;

    private AnyInt32 _bayRecipe;
    private AnyInt32 _rowRecipe;
    private AnyInt32 _tierRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySlotPosition()
        : this(bay:  AnyValidBay(),
               row:  AnyValidRow(),
               tier: AnyValidTier()) { }

    private static AnyInt32 AnyValidBay() {
        return Any.Int32().Positive().LessThanOrEqualTo(99);
    }

    private static AnyInt32 AnyValidRow() {
        return Any.Int32().Between(0, 20);
    }

    private static AnyInt32 AnyValidTier() {
        // Even tiers, 02-20 in the hold or 82-98 on deck.
        return Any.Int32().OneOf(Enumerable.Range(2, 19).Concat(Enumerable.Range(82, 17))).Even();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySlotPosition(AnyInt32 bay,
                            AnyInt32 row,
                            AnyInt32 tier) {
        _bay        = bay;
        _bayRecipe  = bay;
        _row        = row;
        _rowRecipe  = row;
        _tier       = tier;
        _tierRecipe = tier;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySlotPosition(AnySlotPosition from) {
        _bay        = from._bay;
        _bayRecipe  = from._bayRecipe;
        _row        = from._row;
        _rowRecipe  = from._rowRecipe;
        _tier       = from._tier;
        _tierRecipe = from._tierRecipe;
    }

    /// <summary>Pins <c>bay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySlotPosition WithBay(int value) {
        AnySlotPosition copy = new AnySlotPosition(this);

        copy._bay = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bay</c> is drawn from — <c>AnyValidBay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySlotPosition WithBay(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySlotPosition copy = new AnySlotPosition(this);

        copy._bayRecipe = configure(_bayRecipe);
        copy._bay       = copy._bayRecipe;

        return copy;
    }

    /// <summary>Pins <c>row</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySlotPosition WithRow(int value) {
        AnySlotPosition copy = new AnySlotPosition(this);

        copy._row = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>row</c> is drawn from — <c>AnyValidRow()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySlotPosition WithRow(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySlotPosition copy = new AnySlotPosition(this);

        copy._rowRecipe = configure(_rowRecipe);
        copy._row       = copy._rowRecipe;

        return copy;
    }

    /// <summary>Pins <c>tier</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySlotPosition WithTier(int value) {
        AnySlotPosition copy = new AnySlotPosition(this);

        copy._tier = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tier</c> is drawn from — <c>AnyValidTier()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySlotPosition WithTier(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySlotPosition copy = new AnySlotPosition(this);

        copy._tierRecipe = configure(_tierRecipe);
        copy._tier       = copy._tierRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SlotPosition" />.</summary>
    public SlotPosition Generate() {
        return new SlotPosition(_bay.Generate(),
                                _row.Generate(),
                                _tier.Generate());
    }

}
