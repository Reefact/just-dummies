// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TransactionEnvelope --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TransactionEnvelope" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTransactionEnvelope : IAny<TransactionEnvelope> {

    private IAny<Address>     _sender;
    private IAny<UInt128>     _nonce;
    private IAny<ulong>       _gasLimit;
    private IAny<TokenAmount> _gasPrice;
    private IAny<ulong>       _deadlineBlock;

    private AnyAddress     _senderRecipe;
    private AnyUInt128     _nonceRecipe;
    private AnyUInt64      _gasLimitRecipe;
    private AnyTokenAmount _gasPriceRecipe;
    private AnyUInt64      _deadlineBlockRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTransactionEnvelope()
        : this(sender:        AnyValidSender(),
               nonce:         AnyValidNonce(),
               gasLimit:      AnyValidGasLimit(),
               gasPrice:      AnyValidGasPrice(),
               deadlineBlock: AnyValidDeadlineBlock()) { }

    private static AnyAddress AnyValidSender() {
        return new AnyAddress();
    }

    private static AnyUInt128 AnyValidNonce() {
        return Any.UInt128();
    }

    private static AnyUInt64 AnyValidGasLimit() {
        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit);
    }

    private static AnyTokenAmount AnyValidGasPrice() {
        // Whatever the gas limit (or gas used), the fee stays within 2^256 - 1.
        return new AnyTokenAmount().WithBaseUnits(b => b.Between(BigInteger.Zero, TokenAmount.MaxBaseUnits / GasSchedule.BlockGasLimit));
    }

    private static AnyUInt64 AnyValidDeadlineBlock() {
        return Any.UInt64();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTransactionEnvelope(AnyAddress     sender,
                                   AnyUInt128     nonce,
                                   AnyUInt64      gasLimit,
                                   AnyTokenAmount gasPrice,
                                   AnyUInt64      deadlineBlock) {
        _sender              = sender;
        _senderRecipe        = sender;
        _nonce               = nonce;
        _nonceRecipe         = nonce;
        _gasLimit            = gasLimit;
        _gasLimitRecipe      = gasLimit;
        _gasPrice            = gasPrice;
        _gasPriceRecipe      = gasPrice;
        _deadlineBlock       = deadlineBlock;
        _deadlineBlockRecipe = deadlineBlock;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTransactionEnvelope(AnyTransactionEnvelope from) {
        _sender              = from._sender;
        _senderRecipe        = from._senderRecipe;
        _nonce               = from._nonce;
        _nonceRecipe         = from._nonceRecipe;
        _gasLimit            = from._gasLimit;
        _gasLimitRecipe      = from._gasLimitRecipe;
        _gasPrice            = from._gasPrice;
        _gasPriceRecipe      = from._gasPriceRecipe;
        _deadlineBlock       = from._deadlineBlock;
        _deadlineBlockRecipe = from._deadlineBlockRecipe;
    }

    /// <summary>Pins <c>sender</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransactionEnvelope WithSender(Address value) {
        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._sender = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sender</c> is drawn from — <c>AnyValidSender()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAddress, AnyAddress&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransactionEnvelope WithSender(Func<AnyAddress, AnyAddress> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._senderRecipe = configure(_senderRecipe);
        copy._sender       = copy._senderRecipe;

        return copy;
    }

    /// <summary>Pins <c>nonce</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransactionEnvelope WithNonce(UInt128 value) {
        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._nonce = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>nonce</c> is drawn from — <c>AnyValidNonce()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt128, AnyUInt128&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransactionEnvelope WithNonce(Func<AnyUInt128, AnyUInt128> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._nonceRecipe = configure(_nonceRecipe);
        copy._nonce       = copy._nonceRecipe;

        return copy;
    }

    /// <summary>Pins <c>gasLimit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransactionEnvelope WithGasLimit(ulong value) {
        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._gasLimit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gasLimit</c> is drawn from — <c>AnyValidGasLimit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransactionEnvelope WithGasLimit(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._gasLimitRecipe = configure(_gasLimitRecipe);
        copy._gasLimit       = copy._gasLimitRecipe;

        return copy;
    }

    /// <summary>Pins <c>gasPrice</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransactionEnvelope WithGasPrice(TokenAmount value) {
        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._gasPrice = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gasPrice</c> is drawn from — <c>AnyValidGasPrice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTokenAmount, AnyTokenAmount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransactionEnvelope WithGasPrice(Func<AnyTokenAmount, AnyTokenAmount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._gasPriceRecipe = configure(_gasPriceRecipe);
        copy._gasPrice       = copy._gasPriceRecipe;

        return copy;
    }

    /// <summary>Pins <c>deadlineBlock</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransactionEnvelope WithDeadlineBlock(ulong value) {
        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._deadlineBlock = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deadlineBlock</c> is drawn from — <c>AnyValidDeadlineBlock()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransactionEnvelope WithDeadlineBlock(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransactionEnvelope copy = new AnyTransactionEnvelope(this);

        copy._deadlineBlockRecipe = configure(_deadlineBlockRecipe);
        copy._deadlineBlock       = copy._deadlineBlockRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TransactionEnvelope" />.</summary>
    public TransactionEnvelope Generate() {
        return new TransactionEnvelope(_sender.Generate(),
                                       _nonce.Generate(),
                                       _gasLimit.Generate(),
                                       _gasPrice.Generate(),
                                       _deadlineBlock.Generate());
    }

}
