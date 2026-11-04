// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GasSchedule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
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

        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit - GasSchedule.MaxNewHolderSurcharge);
    }

    private static AnyUInt64 AnyValidApprove() {

        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit - GasSchedule.MaxNewHolderSurcharge);
    }

    private static AnyUInt64 AnyValidTransferFrom() {

        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit - GasSchedule.MaxNewHolderSurcharge);
    }

    private static AnyUInt64 AnyValidMint() {

        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit - GasSchedule.MaxNewHolderSurcharge);
    }

    private static AnyUInt64 AnyValidBurn() {

        return Any.UInt64().Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit - GasSchedule.MaxNewHolderSurcharge);
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
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGasSchedule(AnyGasSchedule from) {
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
    }

    /// <summary>Draws <c>transfer</c> and <c>transferFrom</c>, <c>transfer</c> never after <c>transferFrom</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (ulong Transfer, ulong TransferFrom) TransferAndTransferFrom() {
        if (_transferPin is { } transfer) { return (transfer, _transferFromPin ?? _transferFromRecipe.GreaterThanOrEqualTo(transfer).Generate()); }
        if (_transferFromPin is { } transferFrom) { return (_transferRecipe.LessThanOrEqualTo(transferFrom).Generate(), transferFrom); }

        return Any.Pair(_transferRecipe, _transferFromRecipe).Having.Item1LessThanOrEqualToItem2().Generate();
    }

    /// <summary>Pins <c>transfer</c> to a fixed value, which bounds <c>transferFrom</c>; its recipe is kept for a later refinement.</summary>
    public AnyGasSchedule WithTransfer(ulong value) {
        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._transferPin             = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>transfer</c> is drawn from — <c>AnyValidTransfer()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>transferFrom</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGasSchedule WithTransfer(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._transferRecipe          = configure(_transferRecipe);
        copy._transferPin             = null;

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

        return copy;
    }

    /// <summary>Refines the recipe <c>transferFrom</c> is drawn from — <c>AnyValidTransferFrom()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>transfer</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt64, AnyUInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGasSchedule WithTransferFrom(Func<AnyUInt64, AnyUInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGasSchedule copy = new AnyGasSchedule(this);

        copy._transferFromRecipe      = configure(_transferFromRecipe);
        copy._transferFromPin         = null;

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
        (ulong transfer, ulong transferFrom) = TransferAndTransferFrom();

        return new GasSchedule(transfer,
                               _approve.Generate(),
                               transferFrom,
                               _mint.Generate(),
                               _burn.Generate(),
                               _newHolderSurcharge.Generate());
    }

}
