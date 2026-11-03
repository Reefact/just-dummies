// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PaymentBatch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PaymentBatch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPaymentBatch : IAny<PaymentBatch> {

    private IAny<string>                        _messageId;
    private IAny<DateTime>                      _createdAt;
    private IAny<IReadOnlyList<CreditTransfer>> _transfers;

    private AnyString               _messageIdRecipe;
    private AnyDateTime             _createdAtRecipe;
    private IAny<IReadOnlyList<CreditTransfer>> _transfersRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPaymentBatch()
        : this(messageId: AnyValidMessageId(),
               createdAt: AnyValidCreatedAt(),
               transfers: AnyValidTransfers()) { }

    private static AnyString AnyValidMessageId() {
        return Any.String().NotBlank();
    }

    private static AnyDateTime AnyValidCreatedAt() {
        return Any.DateTime();
    }

    private static IAny<IReadOnlyList<CreditTransfer>> AnyValidTransfers() {
        return TransfersFromOneDebtor(transfers => transfers.NonEmpty().WithMaxCount(PaymentBatchLimits.MaximumTransfers));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPaymentBatch(AnyString               messageId,
                            AnyDateTime             createdAt,
                            IAny<IReadOnlyList<CreditTransfer>> transfers) {
        _messageId       = messageId;
        _messageIdRecipe = messageId;
        _createdAt       = createdAt;
        _createdAtRecipe = createdAt;
        _transfers       = transfers;
        _transfersRecipe = transfers;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPaymentBatch(AnyPaymentBatch from) {
        _messageId       = from._messageId;
        _messageIdRecipe = from._messageIdRecipe;
        _createdAt       = from._createdAt;
        _createdAtRecipe = from._createdAtRecipe;
        _transfers       = from._transfers;
        _transfersRecipe = from._transfersRecipe;
    }

    /// <summary>Pins <c>messageId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPaymentBatch WithMessageId(string value) {
        AnyPaymentBatch copy = new AnyPaymentBatch(this);

        copy._messageId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>messageId</c> is drawn from — <c>AnyValidMessageId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPaymentBatch WithMessageId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPaymentBatch copy = new AnyPaymentBatch(this);

        copy._messageIdRecipe = configure(_messageIdRecipe);
        copy._messageId       = copy._messageIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>createdAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPaymentBatch WithCreatedAt(DateTime value) {
        AnyPaymentBatch copy = new AnyPaymentBatch(this);

        copy._createdAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>createdAt</c> is drawn from — <c>AnyValidCreatedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPaymentBatch WithCreatedAt(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPaymentBatch copy = new AnyPaymentBatch(this);

        copy._createdAtRecipe = configure(_createdAtRecipe);
        copy._createdAt       = copy._createdAtRecipe;

        return copy;
    }

    /// <summary>Pins <c>transfers</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPaymentBatch WithTransfers(IReadOnlyList<CreditTransfer> value) {
        AnyPaymentBatch copy = new AnyPaymentBatch(this);

        copy._transfers = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>transfers</c> is drawn from — <c>AnyValidTransfers()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny&lt;IReadOnlyList&lt;CreditTransfer&gt;&gt;, IAny&lt;IReadOnlyList&lt;CreditTransfer&gt;&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPaymentBatch WithTransfers(Func<IAny<IReadOnlyList<CreditTransfer>>, IAny<IReadOnlyList<CreditTransfer>>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPaymentBatch copy = new AnyPaymentBatch(this);

        copy._transfersRecipe = configure(_transfersRecipe);
        copy._transfers       = copy._transfersRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PaymentBatch" />.</summary>
    public PaymentBatch Generate() {
        return new PaymentBatch(_messageId.Generate(),
                                _createdAt.Generate(),
                                _transfers.Generate());
    }

}
