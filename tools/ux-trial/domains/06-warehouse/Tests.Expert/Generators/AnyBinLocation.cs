// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BinLocation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Warehouse.Domain;

namespace Warehouse.Domain.Tests.Generators;

/// <summary>
///     A generator of arbitrary <see cref="BinLocation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBinLocation : IAny<BinLocation> {

    private IAny<char> _aisle;
    private IAny<int>  _rack;
    private IAny<int>  _level;

    private AnyChar  _aisleRecipe;
    private AnyInt32 _rackRecipe;
    private AnyInt32 _levelRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBinLocation()
        : this(aisle: AnyValidAisle(),
               rack:  AnyValidRack(),
               level: AnyValidLevel()) { }

    private static AnyChar AnyValidAisle() {
        return Any.Char().Alpha().InUpperCase();
    }

    private static AnyInt32 AnyValidRack() {
        return Any.Int32().Positive().LessThanOrEqualTo(99);
    }

    private static AnyInt32 AnyValidLevel() {
        return Any.Int32().Between(1, 20);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBinLocation(AnyChar  aisle,
                           AnyInt32 rack,
                           AnyInt32 level) {
        _aisle       = aisle;
        _aisleRecipe = aisle;
        _rack        = rack;
        _rackRecipe  = rack;
        _level       = level;
        _levelRecipe = level;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBinLocation(AnyBinLocation from) {
        _aisle       = from._aisle;
        _aisleRecipe = from._aisleRecipe;
        _rack        = from._rack;
        _rackRecipe  = from._rackRecipe;
        _level       = from._level;
        _levelRecipe = from._levelRecipe;
    }

    /// <summary>Pins <c>aisle</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBinLocation WithAisle(char value) {
        AnyBinLocation copy = new AnyBinLocation(this);

        copy._aisle = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>aisle</c> is drawn from — <c>AnyValidAisle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyChar, AnyChar&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBinLocation WithAisle(Func<AnyChar, AnyChar> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBinLocation copy = new AnyBinLocation(this);

        copy._aisleRecipe = configure(_aisleRecipe);
        copy._aisle       = copy._aisleRecipe;

        return copy;
    }

    /// <summary>Pins <c>rack</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBinLocation WithRack(int value) {
        AnyBinLocation copy = new AnyBinLocation(this);

        copy._rack = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rack</c> is drawn from — <c>AnyValidRack()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBinLocation WithRack(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBinLocation copy = new AnyBinLocation(this);

        copy._rackRecipe = configure(_rackRecipe);
        copy._rack       = copy._rackRecipe;

        return copy;
    }

    /// <summary>Pins <c>level</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBinLocation WithLevel(int value) {
        AnyBinLocation copy = new AnyBinLocation(this);

        copy._level = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>level</c> is drawn from — <c>AnyValidLevel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBinLocation WithLevel(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBinLocation copy = new AnyBinLocation(this);

        copy._levelRecipe = configure(_levelRecipe);
        copy._level       = copy._levelRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BinLocation" />.</summary>
    public BinLocation Generate() {
        return new BinLocation(_aisle.Generate(),
                               _rack.Generate(),
                               _level.Generate());
    }

}
