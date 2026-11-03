// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ledger --force` overwrites it. This type is partial, so members you add in a
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
///     A generator of arbitrary <see cref="Ledger" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLedger : IAny<Ledger> {

    private IAny<TokenDefinition> _token;
    private IAny<Address>         _minter;
    private IAny<Address>         _feeCollector;
    private IAny<TokenAmount>     _initialSupply;
    private IAny<GasSchedule>     _gasSchedule;
    private IAny<TokenAmount>     _minimumGasPrice;
    private IAny<ulong>           _genesisBlock;

    private bool                _initialSupplyPinned;
    private bool                _minimumGasPricePinned;

    private AnyTokenDefinition _tokenRecipe;
    private AnyAddress         _minterRecipe;
    private AnyAddress         _feeCollectorRecipe;
    private AnyTokenAmount     _initialSupplyRecipe;
    private AnyGasSchedule     _gasScheduleRecipe;
    private AnyTokenAmount     _minimumGasPriceRecipe;
    private AnyUInt64          _genesisBlockRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLedger()
        : this(token:           AnyValidToken(),
               minter:          AnyValidMinter(),
               feeCollector:    AnyValidFeeCollector(),
               initialSupply:   AnyValidInitialSupply(),
               gasSchedule:     AnyValidGasSchedule(),
               minimumGasPrice: AnyValidMinimumGasPrice(),
               genesisBlock:    AnyValidGenesisBlock()) { }

    private static AnyTokenDefinition AnyValidToken() {
        return new AnyTokenDefinition();
    }

    private static AnyAddress AnyValidMinter() {
        // The zero address is never a minter nor a collector.
        return new AnyAddress().WithValue(v => v.DifferentFrom(Address.Zero.Value));
    }

    private static AnyAddress AnyValidFeeCollector() {
        // The zero address is never a minter nor a collector.
        return new AnyAddress().WithValue(v => v.DifferentFrom(Address.Zero.Value));
    }

    private static AnyTokenAmount AnyValidInitialSupply() {
        return new AnyTokenAmount();
    }

    private static AnyGasSchedule AnyValidGasSchedule() {
        return new AnyGasSchedule();
    }

    private static AnyTokenAmount AnyValidMinimumGasPrice() {
        return new AnyTokenAmount();
    }

    private static AnyUInt64 AnyValidGenesisBlock() {
        return Any.UInt64();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLedger(AnyTokenDefinition token,
                      AnyAddress         minter,
                      AnyAddress         feeCollector,
                      AnyTokenAmount     initialSupply,
                      AnyGasSchedule     gasSchedule,
                      AnyTokenAmount     minimumGasPrice,
                      AnyUInt64          genesisBlock) {
        _token                 = token;
        _tokenRecipe           = token;
        _minter                = minter;
        _minterRecipe          = minter;
        _feeCollector          = feeCollector;
        _feeCollectorRecipe    = feeCollector;
        _initialSupply         = initialSupply;
        _initialSupplyRecipe   = initialSupply;
        _gasSchedule           = gasSchedule;
        _gasScheduleRecipe     = gasSchedule;
        _minimumGasPrice       = minimumGasPrice;
        _minimumGasPriceRecipe = minimumGasPrice;
        _genesisBlock          = genesisBlock;
        _genesisBlockRecipe    = genesisBlock;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLedger(AnyLedger from) {
        _token                 = from._token;
        _tokenRecipe           = from._tokenRecipe;
        _minter                = from._minter;
        _minterRecipe          = from._minterRecipe;
        _feeCollector          = from._feeCollector;
        _feeCollectorRecipe    = from._feeCollectorRecipe;
        _initialSupply         = from._initialSupply;
        _initialSupplyRecipe   = from._initialSupplyRecipe;
        _gasSchedule           = from._gasSchedule;
        _gasScheduleRecipe     = from._gasScheduleRecipe;
        _minimumGasPrice       = from._minimumGasPrice;
        _minimumGasPriceRecipe = from._minimumGasPriceRecipe;
        _genesisBlock          = from._genesisBlock;
        _genesisBlockRecipe    = from._genesisBlockRecipe;
        _initialSupplyPinned   = from._initialSupplyPinned;
        _minimumGasPricePinned = from._minimumGasPricePinned;
    }

    /// <summary>Pins <c>token</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLedger WithToken(TokenDefinition value) {
        AnyLedger copy = new AnyLedger(this);

        copy._token = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>token</c> is drawn from — <c>AnyValidToken()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTokenDefinition, AnyTokenDefinition&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLedger WithToken(Func<AnyTokenDefinition, AnyTokenDefinition> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._tokenRecipe = configure(_tokenRecipe);
        copy._token       = copy._tokenRecipe;

        return copy;
    }

    /// <summary>Pins <c>minter</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLedger WithMinter(Address value) {
        AnyLedger copy = new AnyLedger(this);

        copy._minter = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minter</c> is drawn from — <c>AnyValidMinter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAddress, AnyAddress&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLedger WithMinter(Func<AnyAddress, AnyAddress> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._minterRecipe = configure(_minterRecipe);
        copy._minter       = copy._minterRecipe;

        return copy;
    }

    /// <summary>Pins <c>feeCollector</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLedger WithFeeCollector(Address value) {
        AnyLedger copy = new AnyLedger(this);

        copy._feeCollector = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>feeCollector</c> is drawn from — <c>AnyValidFeeCollector()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAddress, AnyAddress&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLedger WithFeeCollector(Func<AnyAddress, AnyAddress> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._feeCollectorRecipe = configure(_feeCollectorRecipe);
        copy._feeCollector       = copy._feeCollectorRecipe;

        return copy;
    }

    /// <summary>Pins <c>initialSupply</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLedger WithInitialSupply(TokenAmount value) {
        AnyLedger copy = new AnyLedger(this);

        copy._initialSupply       = Any.Fixed(value);
        copy._initialSupplyPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>initialSupply</c> is drawn from — <c>AnyValidInitialSupply()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTokenAmount, AnyTokenAmount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLedger WithInitialSupply(Func<AnyTokenAmount, AnyTokenAmount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._initialSupplyRecipe = configure(_initialSupplyRecipe);
        copy._initialSupply       = copy._initialSupplyRecipe;

        return copy;
    }

    /// <summary>Pins <c>gasSchedule</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLedger WithGasSchedule(GasSchedule value) {
        AnyLedger copy = new AnyLedger(this);

        copy._gasSchedule = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gasSchedule</c> is drawn from — <c>AnyValidGasSchedule()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGasSchedule, AnyGasSchedule&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLedger WithGasSchedule(Func<AnyGasSchedule, AnyGasSchedule> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._gasScheduleRecipe = configure(_gasScheduleRecipe);
        copy._gasSchedule       = copy._gasScheduleRecipe;

        return copy;
    }

    /// <summary>Pins <c>minimumGasPrice</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLedger WithMinimumGasPrice(TokenAmount value) {
        AnyLedger copy = new AnyLedger(this);

        copy._minimumGasPrice       = Any.Fixed(value);
        copy._minimumGasPricePinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>minimumGasPrice</c> is drawn from — <c>AnyValidMinimumGasPrice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTokenAmount, AnyTokenAmount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLedger WithMinimumGasPrice(Func<AnyTokenAmount, AnyTokenAmount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._minimumGasPriceRecipe = configure(_minimumGasPriceRecipe);
        copy._minimumGasPrice       = copy._minimumGasPriceRecipe;

        return copy;
    }

    /// <summary>Pins <c>genesisBlock</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLedger WithGenesisBlock(ulong value) {
        AnyLedger copy = new AnyLedger(this);

        copy._genesisBlock = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>genesisBlock</c> is drawn from — <c>AnyValidGenesisBlock()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLedger WithGenesisBlock(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLedger copy = new AnyLedger(this);

        copy._genesisBlockRecipe = configure(_genesisBlockRecipe);
        copy._genesisBlock       = copy._genesisBlockRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Ledger" />.</summary>
    public Ledger Generate() {
        TokenDefinition token       = _token.Generate();
        GasSchedule     gasSchedule = _gasSchedule.Generate();

        // The two ties of the constructor, kept by construction: the initial supply fits under the cap, and one
        // transfer at the minimum gas price costs no more than the cap. A pinned amount is handed over as chosen.
        IAny<TokenAmount> initialSupply = _initialSupplyPinned
            ? _initialSupply
            : _initialSupplyRecipe.WithBaseUnits(b => b.LessThanOrEqualTo(token.SupplyCap.BaseUnits));
        IAny<TokenAmount> minimumGasPrice = _minimumGasPricePinned
            ? _minimumGasPrice
            : _minimumGasPriceRecipe.WithBaseUnits(b => b.LessThanOrEqualTo(token.SupplyCap.BaseUnits / gasSchedule.Transfer));

        return new Ledger(token,
                          _minter.Generate(),
                          _feeCollector.Generate(),
                          initialSupply.Generate(),
                          gasSchedule,
                          minimumGasPrice.Generate(),
                          _genesisBlock.Generate());
    }

}
