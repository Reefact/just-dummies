// Scaffolded by `any` (JustDummies), then rewritten by hand: the supply cap is tied to the decimals
// (a whole number of tokens), a rule no per-parameter recipe can honour.

using System;
using System.Numerics;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>A generator of arbitrary <see cref="TokenDefinition" /> values.</summary>
public sealed partial class AnyTokenDefinition : IAny<TokenDefinition> {

    private IAny<string> _name;
    private IAny<string> _symbol;
    private IAny<int>    _decimals;
    private bool         _decimalsPinned;
    private TokenAmount? _pinnedSupplyCap;

    private AnyString      _nameRecipe;
    private AnyPattern     _symbolRecipe;
    private AnyInt32       _decimalsRecipe;
    private AnyBigInteger  _wholeTokensRecipe;

    public AnyTokenDefinition()
        : this(name:        AnyValidName(),
               symbol:      AnyValidSymbol(),
               decimals:    AnyValidDecimals(),
               wholeTokens: AnyValidWholeTokens()) { }

    private static AnyString AnyValidName() {
        return Any.String().AlphaNumeric().WithLengthBetween(1, TokenDefinition.MaxNameLength);
    }

    private static AnyPattern AnyValidSymbol() {
        return Any.StringMatching("[A-Z][A-Z0-9]{1,10}");
    }

    private static AnyInt32 AnyValidDecimals() {
        return Any.Int32().Between(0, TokenDefinition.MaxDecimals);
    }

    // A cap is a whole number of tokens: drawing the count of tokens (never the base units) is what makes it so.
    private static AnyBigInteger AnyValidWholeTokens() {
        return new AnyBigInteger().Between(BigInteger.One, BigInteger.One << 128);
    }

    private AnyTokenDefinition(AnyString name, AnyPattern symbol, AnyInt32 decimals, AnyBigInteger wholeTokens) {
        _name              = name;
        _nameRecipe        = name;
        _symbol            = symbol;
        _symbolRecipe      = symbol;
        _decimals          = decimals;
        _decimalsRecipe    = decimals;
        _wholeTokensRecipe = wholeTokens;
    }

    private AnyTokenDefinition(AnyTokenDefinition from) {
        _name              = from._name;
        _nameRecipe        = from._nameRecipe;
        _symbol            = from._symbol;
        _symbolRecipe      = from._symbolRecipe;
        _decimals          = from._decimals;
        _decimalsRecipe    = from._decimalsRecipe;
        _decimalsPinned    = from._decimalsPinned;
        _wholeTokensRecipe = from._wholeTokensRecipe;
        _pinnedSupplyCap   = from._pinnedSupplyCap;
    }

    public AnyTokenDefinition WithName(string value) {
        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    public AnyTokenDefinition WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    public AnyTokenDefinition WithSymbol(string value) {
        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._symbol = Any.Fixed(value);

        return copy;
    }

    public AnyTokenDefinition WithDecimals(int value) {
        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._decimals       = Any.Fixed(value);
        copy._decimalsPinned = true;

        return copy;
    }

    public AnyTokenDefinition WithDecimals(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._decimalsRecipe = configure(_decimalsRecipe);
        copy._decimals       = copy._decimalsRecipe;

        return copy;
    }

    /// <summary>Pins the cap in whole tokens: the cap is that many times <c>OneToken</c>, whatever the decimals.</summary>
    public AnyTokenDefinition WithWholeTokens(BigInteger value) {
        return WithWholeTokens(b => b.Between(value, value));
    }

    public AnyTokenDefinition WithWholeTokens(Func<AnyBigInteger, AnyBigInteger> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._wholeTokensRecipe = configure(_wholeTokensRecipe);
        copy._pinnedSupplyCap   = null;

        return copy;
    }

    /// <summary>Pins the cap in base units; the decimals then stay within the cap's trailing zeros, unless they are pinned too.</summary>
    public AnyTokenDefinition WithSupplyCap(TokenAmount value) {
        AnyTokenDefinition copy = new AnyTokenDefinition(this);

        copy._pinnedSupplyCap = value;

        return copy;
    }

    public TokenDefinition Generate() {
        string name   = _name.Generate();
        string symbol = _symbol.Generate();

        if (_pinnedSupplyCap is not null) {
            IAny<int> decimals = _decimalsPinned ? _decimals : _decimalsRecipe.LessThanOrEqualTo(TrailingDecimalZeros(_pinnedSupplyCap.BaseUnits));

            return new TokenDefinition(name, symbol, decimals.Generate(), _pinnedSupplyCap);
        }

        int        drawnDecimals = _decimals.Generate();
        BigInteger wholeTokens   = _wholeTokensRecipe.Generate();

        return new TokenDefinition(name, symbol, drawnDecimals, new TokenAmount(wholeTokens * BigInteger.Pow(10, drawnDecimals)));
    }

    private static int TrailingDecimalZeros(BigInteger value) {
        int zeros = 0;
        while (zeros < TokenDefinition.MaxDecimals && !value.IsZero && (value % BigInteger.Pow(10, zeros + 1)).IsZero) {
            zeros++;
        }

        return zeros;
    }

}
