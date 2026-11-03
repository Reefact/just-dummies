// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TransferRecord --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TransferRecord" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTransferRecord : IAny<TransferRecord> {

    private IAny<Address>     _from;
    private IAny<Address>     _to;
    private IAny<TokenAmount> _amount;
    private IAny<ulong>       _blockNumber;

    private AnyAddress     _fromRecipe;
    private AnyAddress     _toRecipe;
    private AnyTokenAmount _amountRecipe;
    private AnyUInt64      _blockNumberRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTransferRecord()
        : this(from:        AnyValidFrom(),
               to:          AnyValidTo(),
               amount:      AnyValidAmount(),
               blockNumber: AnyValidBlockNumber()) { }

    private static AnyAddress AnyValidFrom() {
        // Neither end is the zero address by default (a mint or a burn pins it), so the pair is never zero to zero.
        return new AnyAddress().WithValue(v => v.DifferentFrom(Address.Zero.Value));
    }

    private static AnyAddress AnyValidTo() {
        // Neither end is the zero address by default (a mint or a burn pins it), so the pair is never zero to zero.
        return new AnyAddress().WithValue(v => v.DifferentFrom(Address.Zero.Value));
    }

    private static AnyTokenAmount AnyValidAmount() {
        return new AnyTokenAmount();
    }

    private static AnyUInt64 AnyValidBlockNumber() {
        return Any.UInt64();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTransferRecord(AnyAddress     from,
                              AnyAddress     to,
                              AnyTokenAmount amount,
                              AnyUInt64      blockNumber) {
        _from              = from;
        _fromRecipe        = from;
        _to                = to;
        _toRecipe          = to;
        _amount            = amount;
        _amountRecipe      = amount;
        _blockNumber       = blockNumber;
        _blockNumberRecipe = blockNumber;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTransferRecord(AnyTransferRecord from) {
        _from              = from._from;
        _fromRecipe        = from._fromRecipe;
        _to                = from._to;
        _toRecipe          = from._toRecipe;
        _amount            = from._amount;
        _amountRecipe      = from._amountRecipe;
        _blockNumber       = from._blockNumber;
        _blockNumberRecipe = from._blockNumberRecipe;
    }

    /// <summary>Pins <c>from</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransferRecord WithFrom(Address value) {
        AnyTransferRecord copy = new AnyTransferRecord(this);

        copy._from = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>from</c> is drawn from — <c>AnyValidFrom()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAddress, AnyAddress&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransferRecord WithFrom(Func<AnyAddress, AnyAddress> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransferRecord copy = new AnyTransferRecord(this);

        copy._fromRecipe = configure(_fromRecipe);
        copy._from       = copy._fromRecipe;

        return copy;
    }

    /// <summary>Pins <c>to</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransferRecord WithTo(Address value) {
        AnyTransferRecord copy = new AnyTransferRecord(this);

        copy._to = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>to</c> is drawn from — <c>AnyValidTo()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAddress, AnyAddress&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransferRecord WithTo(Func<AnyAddress, AnyAddress> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransferRecord copy = new AnyTransferRecord(this);

        copy._toRecipe = configure(_toRecipe);
        copy._to       = copy._toRecipe;

        return copy;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransferRecord WithAmount(TokenAmount value) {
        AnyTransferRecord copy = new AnyTransferRecord(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTokenAmount, AnyTokenAmount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransferRecord WithAmount(Func<AnyTokenAmount, AnyTokenAmount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransferRecord copy = new AnyTransferRecord(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Pins <c>blockNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransferRecord WithBlockNumber(ulong value) {
        AnyTransferRecord copy = new AnyTransferRecord(this);

        copy._blockNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>blockNumber</c> is drawn from — <c>AnyValidBlockNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransferRecord WithBlockNumber(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransferRecord copy = new AnyTransferRecord(this);

        copy._blockNumberRecipe = configure(_blockNumberRecipe);
        copy._blockNumber       = copy._blockNumberRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TransferRecord" />.</summary>
    public TransferRecord Generate() {
        return new TransferRecord(_from.Generate(),
                                  _to.Generate(),
                                  _amount.Generate(),
                                  _blockNumber.Generate());
    }

}
