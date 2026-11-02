// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DataBundle --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Telecom.Domain;

namespace Telecom.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="DataBundle" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDataBundle : IAny<DataBundle> {

    private IAny<int>      _megabytes;
    private IAny<decimal>  _price;
    private IAny<TimeSpan> _validity;

    private AnyInt32    _megabytesRecipe;
    private AnyDecimal  _priceRecipe;
    private AnyTimeSpan _validityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDataBundle()
        : this(megabytes: AnyValidMegabytes(),
               price:     AnyValidPrice(),
               validity:  AnyValidValidity()) { }

    private static AnyInt32 AnyValidMegabytes() {
        return Any.Int32().Between(100, 100000).MultipleOf(100);
    }

    private static AnyDecimal AnyValidPrice() {
        return Any.Decimal().Between(0.01m, 500m);
    }

    private static AnyTimeSpan AnyValidValidity() {
        return Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.FromDays(1));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDataBundle(AnyInt32    megabytes,
                          AnyDecimal  price,
                          AnyTimeSpan validity) {
        _megabytes       = megabytes;
        _megabytesRecipe = megabytes;
        _price           = price;
        _priceRecipe     = price;
        _validity        = validity;
        _validityRecipe  = validity;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDataBundle(AnyDataBundle from) {
        _megabytes       = from._megabytes;
        _megabytesRecipe = from._megabytesRecipe;
        _price           = from._price;
        _priceRecipe     = from._priceRecipe;
        _validity        = from._validity;
        _validityRecipe  = from._validityRecipe;
    }

    /// <summary>Pins <c>megabytes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDataBundle WithMegabytes(int value) {
        AnyDataBundle copy = new AnyDataBundle(this);

        copy._megabytes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>megabytes</c> is drawn from — <c>AnyValidMegabytes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDataBundle WithMegabytes(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDataBundle copy = new AnyDataBundle(this);

        copy._megabytesRecipe = configure(_megabytesRecipe);
        copy._megabytes       = copy._megabytesRecipe;

        return copy;
    }

    /// <summary>Pins <c>price</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDataBundle WithPrice(decimal value) {
        AnyDataBundle copy = new AnyDataBundle(this);

        copy._price = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>price</c> is drawn from — <c>AnyValidPrice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDataBundle WithPrice(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDataBundle copy = new AnyDataBundle(this);

        copy._priceRecipe = configure(_priceRecipe);
        copy._price       = copy._priceRecipe;

        return copy;
    }

    /// <summary>Pins <c>validity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDataBundle WithValidity(TimeSpan value) {
        AnyDataBundle copy = new AnyDataBundle(this);

        copy._validity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>validity</c> is drawn from — <c>AnyValidValidity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDataBundle WithValidity(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDataBundle copy = new AnyDataBundle(this);

        copy._validityRecipe = configure(_validityRecipe);
        copy._validity       = copy._validityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="DataBundle" />.</summary>
    public DataBundle Generate() {
        return new DataBundle(_megabytes.Generate(),
                              _price.Generate(),
                              _validity.Generate());
    }

}
