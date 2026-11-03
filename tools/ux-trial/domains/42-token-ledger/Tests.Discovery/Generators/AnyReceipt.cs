// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Receipt --force` overwrites it. This type is partial, so members you add in a
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
///     A generator of arbitrary <see cref="Receipt" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReceipt : IAny<Receipt> {

    private IAny<TransactionKind> _kind;
    private IAny<Address>         _sender;
    private IAny<UInt128>         _nonce;
    private IAny<ulong>           _blockNumber;
    private IAny<ulong>           _gasUsed;
    private IAny<TokenAmount>     _gasPrice;
    private TokenAmount?          _gasPricePin;

    private AnyEnum<TransactionKind> _kindRecipe;
    private AnyAddress               _senderRecipe;
    private AnyUInt128               _nonceRecipe;
    private AnyUInt64                _blockNumberRecipe;
    private AnyUInt64                _gasUsedRecipe;
    private AnyTokenAmount           _gasPriceRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReceipt()
        : this(kind:        AnyValidKind(),
               sender:      AnyValidSender(),
               nonce:       AnyValidNonce(),
               blockNumber: AnyValidBlockNumber(),
               gasUsed:     AnyValidGasUsed(),
               gasPrice:    AnyValidGasPrice()) { }

    private static AnyEnum<TransactionKind> AnyValidKind() {
        return Any.Enum<TransactionKind>();
    }

    private static AnyAddress AnyValidSender() {
        return new AnyAddress().NonZero();
    }

    private static AnyUInt128 AnyValidNonce() {
        return Any.UInt128();
    }

    private static AnyUInt64 AnyValidBlockNumber() {
        return Any.UInt64();
    }

    private static AnyUInt64 AnyValidGasUsed() {
        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit);
    }

    private static AnyTokenAmount AnyValidGasPrice() {
        return new AnyTokenAmount();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReceipt(AnyEnum<TransactionKind> kind,
                       AnyAddress               sender,
                       AnyUInt128               nonce,
                       AnyUInt64                blockNumber,
                       AnyUInt64                gasUsed,
                       AnyTokenAmount           gasPrice) {
        _kind              = kind;
        _kindRecipe        = kind;
        _sender            = sender;
        _senderRecipe      = sender;
        _nonce             = nonce;
        _nonceRecipe       = nonce;
        _blockNumber       = blockNumber;
        _blockNumberRecipe = blockNumber;
        _gasUsed           = gasUsed;
        _gasUsedRecipe     = gasUsed;
        _gasPrice          = gasPrice;
        _gasPriceRecipe    = gasPrice;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReceipt(AnyReceipt from) {
        _kind              = from._kind;
        _kindRecipe        = from._kindRecipe;
        _sender            = from._sender;
        _senderRecipe      = from._senderRecipe;
        _nonce             = from._nonce;
        _nonceRecipe       = from._nonceRecipe;
        _blockNumber       = from._blockNumber;
        _blockNumberRecipe = from._blockNumberRecipe;
        _gasUsed           = from._gasUsed;
        _gasUsedRecipe     = from._gasUsedRecipe;
        _gasPrice          = from._gasPrice;
        _gasPricePin       = from._gasPricePin;
        _gasPriceRecipe    = from._gasPriceRecipe;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceipt WithKind(TransactionKind value) {
        AnyReceipt copy = new AnyReceipt(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;TransactionKind&gt;, AnyEnum&lt;TransactionKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceipt WithKind(Func<AnyEnum<TransactionKind>, AnyEnum<TransactionKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceipt copy = new AnyReceipt(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>sender</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceipt WithSender(Address value) {
        AnyReceipt copy = new AnyReceipt(this);

        copy._sender = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sender</c> is drawn from — <c>AnyValidSender()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAddress, AnyAddress&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceipt WithSender(Func<AnyAddress, AnyAddress> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceipt copy = new AnyReceipt(this);

        copy._senderRecipe = configure(_senderRecipe);
        copy._sender       = copy._senderRecipe;

        return copy;
    }

    /// <summary>Pins <c>nonce</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceipt WithNonce(UInt128 value) {
        AnyReceipt copy = new AnyReceipt(this);

        copy._nonce = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>nonce</c> is drawn from — <c>AnyValidNonce()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt128, AnyUInt128&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceipt WithNonce(Func<AnyUInt128, AnyUInt128> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceipt copy = new AnyReceipt(this);

        copy._nonceRecipe = configure(_nonceRecipe);
        copy._nonce       = copy._nonceRecipe;

        return copy;
    }

    /// <summary>Pins <c>blockNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceipt WithBlockNumber(ulong value) {
        AnyReceipt copy = new AnyReceipt(this);

        copy._blockNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>blockNumber</c> is drawn from — <c>AnyValidBlockNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceipt WithBlockNumber(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceipt copy = new AnyReceipt(this);

        copy._blockNumberRecipe = configure(_blockNumberRecipe);
        copy._blockNumber       = copy._blockNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>gasUsed</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceipt WithGasUsed(ulong value) {
        AnyReceipt copy = new AnyReceipt(this);

        copy._gasUsed = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gasUsed</c> is drawn from — <c>AnyValidGasUsed()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceipt WithGasUsed(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceipt copy = new AnyReceipt(this);

        copy._gasUsedRecipe = configure(_gasUsedRecipe);
        copy._gasUsed       = copy._gasUsedRecipe;

        return copy;
    }

    /// <summary>Pins <c>gasPrice</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceipt WithGasPrice(TokenAmount value) {
        AnyReceipt copy = new AnyReceipt(this);

        copy._gasPricePin = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>gasPrice</c> is drawn from — <c>AnyValidGasPrice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTokenAmount, AnyTokenAmount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceipt WithGasPrice(Func<AnyTokenAmount, AnyTokenAmount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceipt copy = new AnyReceipt(this);

        copy._gasPriceRecipe = configure(_gasPriceRecipe);
        copy._gasPrice       = copy._gasPriceRecipe;
        copy._gasPricePin = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Receipt" />.</summary>
    public Receipt Generate() {
        TransactionKind kind        = _kind.Generate();
        Address         sender      = _sender.Generate();
        UInt128         nonce       = _nonce.Generate();
        ulong           blockNumber = _blockNumber.Generate();
        ulong           gasUsed     = _gasUsed.Generate();
        TokenAmount     gasPrice    = _gasPricePin ?? FitUnder(_gasPrice.Generate(), gasUsed);

        return new Receipt(kind, sender, nonce, blockNumber, gasUsed, gasPrice);
    }

    /// <summary>The constructor ties the price to the gas used (gas x price fits 256 bits): a drawn price is folded under that ceiling, a pinned one is left to be refused.</summary>
    private static TokenAmount FitUnder(TokenAmount drawn, ulong gasUsed) {
        BigInteger ceiling = TokenAmount.MaxBaseUnits / gasUsed;

        return new TokenAmount(drawn.BaseUnits % (ceiling + 1));
    }

}
