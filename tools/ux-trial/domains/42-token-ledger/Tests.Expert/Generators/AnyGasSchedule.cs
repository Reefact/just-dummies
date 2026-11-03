// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GasSchedule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GasSchedule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGasSchedule : IAny<GasSchedule> {

    private AnyOrderedPair<ulong> _transferAndTransferFrom;
    private IAny<ulong>           _approve;
    private IAny<ulong>           _mint;
    private IAny<ulong>           _burn;
    private IAny<ulong>           _newHolderSurcharge;

    private AnyUInt64 _transferRecipe;
    private AnyUInt64 _approveRecipe;
    private AnyUInt64 _transferFromRecipe;
    private AnyUInt64 _mintRecipe;
    private AnyUInt64 _burnRecipe;
    private AnyUInt64 _newHolderSurchargeRecipe;

    private bool   _surchargePinned;

    private ulong? _transferPin;
    private ulong? _transferFromPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGasSchedule()
        : this(transfer:           AnyValidTransfer(),
               approve:            AnyValidApprove(),
               transferFrom:       AnyValidTransferFrom(),
               mint:               AnyValidMint(),
               burn:               AnyValidBurn(),
               newHolderSurcharge: AnyValidNewHolderSurcharge()) { }

    private static AnyUInt64 AnyValidTransfer() {
        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit);
    }

    private static AnyUInt64 AnyValidApprove() {
        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit);
    }

    private static AnyUInt64 AnyValidTransferFrom() {
        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit);
    }

    private static AnyUInt64 AnyValidMint() {
        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit);
    }

    private static AnyUInt64 AnyValidBurn() {
        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit);
    }

    private static AnyUInt64 AnyValidNewHolderSurcharge() {
        return Any.UInt64().LessThanOrEqualTo(GasSchedule.MaxNewHolderSurcharge);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGasSchedule(AnyUInt64 transfer,
                           AnyUInt64 approve,
                           AnyUInt64 transferFrom,
                           AnyUInt64 mint,
                           AnyUInt64 burn,
                           AnyUInt64 newHolderSurcharge) {
        _transferRecipe           = transfer;
        _approve                  = approve;
        _approveRecipe            = approve;
        _transferFromRecipe       = transferFrom;
        _mint                     = mint;
        _mintRecipe               = mint;
        _burn                     = burn;
        _burnRecipe               = burn;
        _newHolderSurcharge       = newHolderSurcharge;
        _newHolderSurchargeRecipe = newHolderSurcharge;
        _transferAndTransferFrom  = TransferAndTransferFrom();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGasSchedule(AnyGasSchedule from) {
        _transferAndTransferFrom  = from._transferAndTransferFrom;
        _transferRecipe           = from._transferRecipe;
        _transferPin              = from._transferPin;
        _approve                  = from._approve;
        _approveRecipe            = from._approveRecipe;
        _transferFromRecipe       = from._transferFromRecipe;
        _transferFromPin          = from._transferFromPin;
        _mint                     = from._mint;
        _mintRecipe               = from._mintRecipe;
        _burn                     = from._burn;
        _burnRecipe               = from._burnRecipe;
        _newHolderSurcharge       = from._newHolderSurcharge;
        _newHolderSurchargeRecipe = from._newHolderSurchargeRecipe;
        _surchargePinned          = from._surchargePinned;
    }

    /// <summary>Draws <c>transfer</c> and <c>transferFrom</c> together, <c>transfer</c> never after <c>transferFrom</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<ulong> TransferAndTransferFrom() {
        AnyOrderedPair<ulong> pair = Any.OrderedPair(_transferRecipe, _transferFromRecipe);

        if (_transferPin is { } transfer) { pair = pair.WithFirst(transfer); }
        if (_transferFromPin is { } transferFrom) { pair = pair.WithSecond(transferFrom); }

        return pair;
    }

    /// <summary>Pins <c>transfer</c> to a fixed value, which bounds <c>transferFrom</c>; its recipe is kept for a later refinement.</summary>
    public AnyGasSchedule WithTransfer(ulong value) {
        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._transferPin             = value;
        copy._transferAndTransferFrom = copy.TransferAndTransferFrom();

        return copy;
    }

    /// <summary>Refines the recipe <c>transfer</c> is drawn from — <c>AnyValidTransfer()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>transferFrom</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGasSchedule WithTransfer(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._transferRecipe          = configure(_transferRecipe);
        copy._transferPin             = null;
        copy._transferAndTransferFrom = copy.TransferAndTransferFrom();

        return copy;
    }

    /// <summary>Pins <c>approve</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGasSchedule WithApprove(ulong value) {
        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._approve = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>approve</c> is drawn from — <c>AnyValidApprove()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGasSchedule WithApprove(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._approveRecipe = configure(_approveRecipe);
        copy._approve       = copy._approveRecipe;

        return copy;
    }

    /// <summary>Pins <c>transferFrom</c> to a fixed value, which bounds <c>transfer</c>; its recipe is kept for a later refinement.</summary>
    public AnyGasSchedule WithTransferFrom(ulong value) {
        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._transferFromPin         = value;
        copy._transferAndTransferFrom = copy.TransferAndTransferFrom();

        return copy;
    }

    /// <summary>Refines the recipe <c>transferFrom</c> is drawn from — <c>AnyValidTransferFrom()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>transfer</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGasSchedule WithTransferFrom(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._transferFromRecipe      = configure(_transferFromRecipe);
        copy._transferFromPin         = null;
        copy._transferAndTransferFrom = copy.TransferAndTransferFrom();

        return copy;
    }

    /// <summary>Pins <c>mint</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGasSchedule WithMint(ulong value) {
        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._mint = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>mint</c> is drawn from — <c>AnyValidMint()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGasSchedule WithMint(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._mintRecipe = configure(_mintRecipe);
        copy._mint       = copy._mintRecipe;

        return copy;
    }

    /// <summary>Pins <c>burn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGasSchedule WithBurn(ulong value) {
        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._burn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>burn</c> is drawn from — <c>AnyValidBurn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGasSchedule WithBurn(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._burnRecipe = configure(_burnRecipe);
        copy._burn       = copy._burnRecipe;

        return copy;
    }

    /// <summary>Pins <c>newHolderSurcharge</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGasSchedule WithNewHolderSurcharge(ulong value) {
        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._newHolderSurcharge = Any.Fixed(value);
        copy._surchargePinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>newHolderSurcharge</c> is drawn from — <c>AnyValidNewHolderSurcharge()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGasSchedule WithNewHolderSurcharge(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._newHolderSurchargeRecipe = configure(_newHolderSurchargeRecipe);
        copy._newHolderSurcharge       = copy._newHolderSurchargeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="GasSchedule" />.</summary>
    public GasSchedule Generate() {
        (ulong transfer, ulong transferFrom) = _transferAndTransferFrom.Generate();
        ulong mint = _mint.Generate();

        // A transfer or a mint to a new holder must still fit in a block: the surcharge a draw adds is kept under
        // what is left. A pinned surcharge is handed over as chosen, and the constructor refuses it if it does not fit.
        ulong                crediting = Math.Max(Math.Max(transfer, transferFrom), mint);
        IAny<ulong>          surcharge = _surchargePinned
            ? _newHolderSurcharge
            : _newHolderSurchargeRecipe.LessThanOrEqualTo(GasSchedule.BlockGasLimit - crediting);

        return new GasSchedule(transfer,
                               _approve.Generate(),
                               transferFrom,
                               mint,
                               _burn.Generate(),
                               surcharge.Generate());
    }

}
