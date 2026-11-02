// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate UsageRecord --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="UsageRecord" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyUsageRecord : IAny<UsageRecord> {

    private IAny<DateTime>  _occurredAt;
    private IAny<UsageKind> _kind;
    private IAny<long>      _quantity;

    private AnyDateTime        _occurredAtRecipe;
    private AnyEnum<UsageKind> _kindRecipe;
    private AnyInt64           _quantityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyUsageRecord()
        : this(occurredAt: AnyValidOccurredAt(),
               kind:       AnyValidKind(),
               quantity:   AnyValidQuantity()) { }

    private static AnyDateTime AnyValidOccurredAt() {
        return Any.DateTime();
    }

    private static AnyEnum<UsageKind> AnyValidKind() {
        return Any.Enum<UsageKind>();
    }

    private static AnyInt64 AnyValidQuantity() {
        return Any.Int64().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyUsageRecord(AnyDateTime        occurredAt,
                           AnyEnum<UsageKind> kind,
                           AnyInt64           quantity) {
        _occurredAt       = occurredAt;
        _occurredAtRecipe = occurredAt;
        _kind             = kind;
        _kindRecipe       = kind;
        _quantity         = quantity;
        _quantityRecipe   = quantity;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyUsageRecord(AnyUsageRecord from) {
        _occurredAt       = from._occurredAt;
        _occurredAtRecipe = from._occurredAtRecipe;
        _kind             = from._kind;
        _kindRecipe       = from._kindRecipe;
        _quantity         = from._quantity;
        _quantityRecipe   = from._quantityRecipe;
    }

    /// <summary>Pins <c>occurredAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyUsageRecord WithOccurredAt(DateTime value) {
        AnyUsageRecord copy = new AnyUsageRecord(this);

        copy._occurredAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>occurredAt</c> is drawn from — <c>AnyValidOccurredAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyUsageRecord WithOccurredAt(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyUsageRecord copy = new AnyUsageRecord(this);

        copy._occurredAtRecipe = configure(_occurredAtRecipe);
        copy._occurredAt       = copy._occurredAtRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyUsageRecord WithKind(UsageKind value) {
        AnyUsageRecord copy = new AnyUsageRecord(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;UsageKind&gt;, AnyEnum&lt;UsageKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyUsageRecord WithKind(Func<AnyEnum<UsageKind>, AnyEnum<UsageKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyUsageRecord copy = new AnyUsageRecord(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>quantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyUsageRecord WithQuantity(long value) {
        AnyUsageRecord copy = new AnyUsageRecord(this);

        copy._quantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quantity</c> is drawn from — <c>AnyValidQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt64, AnyInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyUsageRecord WithQuantity(Func<AnyInt64, AnyInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyUsageRecord copy = new AnyUsageRecord(this);

        copy._quantityRecipe = configure(_quantityRecipe);
        copy._quantity       = copy._quantityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="UsageRecord" />.</summary>
    public UsageRecord Generate() {
        DateTime  occurredAt = _occurredAt.Generate();
        UsageKind kind       = _kind.Generate();

        // An SMS counts exactly one message: the rule ties quantity to kind, so quantity is drawn
        // only for the kinds that meter an amount.
        long quantity = kind == UsageKind.Sms ? 1L : _quantity.Generate();

        return new UsageRecord(occurredAt, kind, quantity);
    }

}
