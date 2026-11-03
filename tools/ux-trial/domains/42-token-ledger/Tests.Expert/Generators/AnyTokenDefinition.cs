// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TokenDefinition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Numerics;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TokenDefinition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTokenDefinition : IAny<TokenDefinition> {

    private IAny<string>      _name;
    private IAny<string>      _symbol;
    private IAny<int>         _decimals;
    private IAny<TokenAmount> _supplyCap;

    private bool          _supplyCapPinned;

    private AnyPattern     _nameRecipe;
    private AnyPattern     _symbolRecipe;
    private AnyInt32       _decimalsRecipe;
    private AnyTokenAmount _supplyCapRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTokenDefinition()
        : this(name:      AnyValidName(),
               symbol:    AnyValidSymbol(),
               decimals:  AnyValidDecimals(),
               supplyCap: AnyValidSupplyCap()) { }

    private static AnyPattern AnyValidName() {
        // 1 to 50 printable characters, the first and the last not a space (a one-character name included).
        return Any.StringMatching("[!-~]([ -~]{0,48}[!-~])?");
    }

    private static AnyPattern AnyValidSymbol() {
        return Any.StringMatching("[A-Z][A-Z0-9]{1,10}");
    }

    private static AnyInt32 AnyValidDecimals() {
        return Any.Int32().Between(0, TokenDefinition.MaxDecimals);
    }

    private static AnyTokenAmount AnyValidSupplyCap() {
        // At least 10^36 base units, so that rounding down to a whole number of tokens leaves one token or more at any decimals.
        return new AnyTokenAmount().WithBaseUnits(b => b.Between(BigInteger.Pow(10, TokenDefinition.MaxDecimals), BigInteger.Pow(10, 50)));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTokenDefinition(AnyPattern     name,
                               AnyPattern     symbol,
                               AnyInt32       decimals,
                               AnyTokenAmount supplyCap) {
        _name            = name;
        _nameRecipe      = name;
        _symbol          = symbol;
        _symbolRecipe    = symbol;
        _decimals        = decimals;
        _decimalsRecipe  = decimals;
        _supplyCap       = supplyCap;
        _supplyCapRecipe = supplyCap;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTokenDefinition(AnyTokenDefinition from) {
        _name            = from._name;
        _nameRecipe      = from._nameRecipe;
        _symbol          = from._symbol;
        _symbolRecipe    = from._symbolRecipe;
        _decimals        = from._decimals;
        _decimalsRecipe  = from._decimalsRecipe;
        _supplyCap       = from._supplyCap;
        _supplyCapRecipe = from._supplyCapRecipe;
        _supplyCapPinned = from._supplyCapPinned;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenDefinition WithName(string value) {
        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenDefinition WithName(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>symbol</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenDefinition WithSymbol(string value) {
        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._symbol = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>symbol</c> is drawn from — <c>AnyValidSymbol()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenDefinition WithSymbol(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._symbolRecipe = configure(_symbolRecipe);
        copy._symbol       = copy._symbolRecipe;

        return copy;
    }

    /// <summary>Pins <c>decimals</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenDefinition WithDecimals(int value) {
        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._decimals = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>decimals</c> is drawn from — <c>AnyValidDecimals()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenDefinition WithDecimals(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._decimalsRecipe = configure(_decimalsRecipe);
        copy._decimals       = copy._decimalsRecipe;

        return copy;
    }

    /// <summary>Pins <c>supplyCap</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTokenDefinition WithSupplyCap(TokenAmount value) {
        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._supplyCap       = Any.Fixed(value);
        copy._supplyCapPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>supplyCap</c> is drawn from — <c>AnyValidSupplyCap()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTokenAmount, AnyTokenAmount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTokenDefinition WithSupplyCap(Func<AnyTokenAmount, AnyTokenAmount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._supplyCapRecipe = configure(_supplyCapRecipe);
        copy._supplyCap       = copy._supplyCapRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TokenDefinition" />.</summary>
    public TokenDefinition Generate() {
        int         decimals  = _decimals.Generate();
        TokenAmount supplyCap = _supplyCap.Generate();

        if (!_supplyCapPinned) {
            // The constructor wants a whole number of tokens, which the cap's recipe cannot know:
            // round the drawn amount down to one. A pinned cap is handed over as chosen, and refused if it is not whole.
            BigInteger oneToken = BigInteger.Pow(10, decimals);

            supplyCap = new TokenAmount(supplyCap.BaseUnits / oneToken * oneToken);
        }

        return new TokenDefinition(_name.Generate(),
                                   _symbol.Generate(),
                                   decimals,
                                   supplyCap);
    }

}
